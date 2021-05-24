// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Crossfire.Background.Common;
using Crossfire.Config;
using Crossfire.Extensions;
using Crossfire.Model.Metadata;
using Crossfire.SignalR.Hubs;
using Crossfire.Storage;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using PyrosWeb.Extensions;

namespace Crossfire
{
    /// <summary>
    /// Application startup behaviour.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="env">Application environment.</param>
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            this.HostingEnvironment = env;
            this.Configuration = builder.Build();
        }

        /// <summary>
        /// Gets application config from appsettings.json.
        /// </summary>
        public IConfigurationRoot Configuration { get; }

        private IWebHostEnvironment HostingEnvironment { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.

        /// <summary>
        /// Standard DI configurator for .NET Core 3.1.
        /// </summary>
        /// <param name="services">Application services.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            var queryProcessorConfig = this.Configuration.GetSection("QueryProcessor").Get<QueryProcessorConfig>();
            var cacheConfig = this.Configuration.GetSection("DistributedCache").Get<DistributedCacheConfig>();
            var authConfig = this.Configuration.GetSection("Authentication").Get<AuthenticationConfig>();
            var connectedServersConfig = this.Configuration.GetSection("ConnectedServers").Get<ConnectedServerConfig[]>();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(jwtOptions =>
            {
                jwtOptions.Authority = $"{authConfig.JwtAuthorityBase}/{authConfig.AzureAdTenant}/{authConfig.AzureAdPolicy}/v2.0/";
                jwtOptions.Audience = authConfig.AzureAdClientId;
            });

            services.AddCors(options => options.AddPolicy(
                "CorsPolicy",
                builder =>
            {
                var origins = new List<string> { authConfig.AllowedOrigin };

                if (this.HostingEnvironment.IsDevelopment())
                {
                    origins.Add("https://localhost");
                }

                builder
                .WithMethods(HttpMethod.Post.Method, HttpMethod.Get.Method)
                .AllowAnyHeader()
                .WithOrigins(origins.ToArray())
                .AllowCredentials();
            }));

            services.AddHttpClient<AzureAdB2CGraphClient>();
            services.AddSingleton(authConfig);

            services.AddSingleton(provider =>
            {
                var storageContextLogger = provider.GetRequiredService<ILogger<StorageContext>>();
                return new StorageContext(
                    IdentityManager.SecureStringToString(IdentityManager.GetSecret(
                    keyVaultUri: authConfig.KeyVaultUri,
                    secretId: authConfig.Storage,
                    tenantId: authConfig.AzureAdTenant)
                    .Result),
                    logger: storageContextLogger);
            });

            services.AddSingleton(provider =>
            {
                var storageContext = provider.GetRequiredService<StorageContext>();
                return new CachedEntityClient<string>(
                    context: storageContext,
                    cacheTableName: cacheConfig.TokenStore);
            });

            services.AddSingleton(provider =>
            {
                var context = provider.GetRequiredService<StorageContext>();
                return new CachedEntityClient<UserConnectionInfo[]>(
                    context: context,
                    cacheTableName: cacheConfig.ConnectionInfoStore);
            });

            services.AddMemoryCache((options) =>
            {
                options.SizeLimit = queryProcessorConfig.CacheMaxSize;
            });
            services.AddSingleton(new BackgroundJobLauncher(connectedServersConfig, authConfig.KeyVaultUri, queryProcessorConfig));

            services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseSqlServerStorage(this.Configuration["ConnectionStrings:HangfireDatabase"], new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                UsePageLocksOnDequeue = true,
                DisableGlobalLocks = true,
                EnableHeavyMigrations = false,
            }));

            // direct copy from https://github.com/HangfireIO/Hangfire/blob/a604028fa8a75ea1a122f58464e0823a076e6431/src/Hangfire.AspNetCore/HangfireServiceCollectionExtensions.cs
            services.AddTransient<Microsoft.Extensions.Hosting.IHostedService, BackgroundJobServerHostedService>(provider =>
            {
                ThrowIfNotConfigured(provider);

                var options = provider.GetService<BackgroundJobServerOptions>() ?? new BackgroundJobServerOptions();

                // create a default queue equal to server name
                // doing this prevents other instances from picking up tasks created by other sticky sessions
                options.Queues = queryProcessorConfig.GetQueues(System.Net.Dns.GetHostName());
                options.WorkerCount = queryProcessorConfig.HangfireWorkers;

                var storage = provider.GetService<JobStorage>() ?? JobStorage.Current;
                var additionalProcesses = provider.GetServices<IBackgroundProcess>();

                options.Activator ??= provider.GetService<JobActivator>();
                options.FilterProvider ??= provider.GetService<IJobFilterProvider>();
                options.TimeZoneResolver ??= provider.GetService<ITimeZoneResolver>();

                return new BackgroundJobServerHostedService(storage, options, additionalProcesses);
            });

            services.AddSignalR().AddAzureSignalR(options =>
            {
                string signalRKey = IdentityManager.SecureStringToString(IdentityManager.GetSecret(
                    keyVaultUri: authConfig.KeyVaultUri,
                    secretId: authConfig.AzureSignalRKey,
                    tenantId: authConfig.AzureAdTenant)
                    .Result);

                options.ConnectionString = $"Endpoint={authConfig.AzureSignalREndpoint};AccessKey={signalRKey};Version=1.0;";
                options.GracefulShutdown = new GracefulShutdownOptions
                {
                    Mode = GracefulShutdownMode.WaitForClientsClose,
                    Timeout = TimeSpan.FromSeconds(15),
                };
                options.ServerStickyMode = ServerStickyMode.Required;
            });

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1.1.0", new OpenApiInfo { Title = "Crossfire Data Acquisition API", Version = "v1.1.0" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                });
                c.OperationFilter<AuthorizationHeaderOperationFilter>();
            });

            services.AddApplicationInsightsTelemetry((options) =>
            {
                options.DeveloperMode = this.HostingEnvironment.IsDevelopment();
                options.InstrumentationKey = this.Configuration.GetSection("ApplicationInsights").GetValue<string>("InstrumentationKey");
                options.EnableAuthenticationTrackingJavaScript = false;
                options.EnableDebugLogger = false;
            });

            services.AddLogging((builder) =>
            {
                builder.AddConsole(options => this.Configuration.GetSection("Logging"));
            });

            services.AddHealthChecks();

            // Add framework services.
            services.AddMvc().AddNewtonsoftJson(setup =>
            {
                setup.SerializerSettings.Converters.Add(new StringEnumConverter());
            }).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.

        /// <summary>
        /// Application configurator (.NET Core 3.1).
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <param name="env">Hosting envrionment.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // enable job dashboard
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter() },
            });

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1.1.0/swagger.json", "Crossfire Data Acquisition API v1.1.0");
                c.RoutePrefix = string.Empty;
                c.EnableValidator(null);
            });

            app.UseCors("CorsPolicy");

            app.UseAzureSignalR(routes =>
            {
                routes.MapHub<ModelMessageHub>($"/{ModelMessageHub.HubName}");
            });

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }

        /// <summary>
        /// Helper method for Hangifre/AspnetCore configurator.
        /// </summary>
        /// <param name="serviceProvider">DI container reference.</param>
        internal static void ThrowIfNotConfigured(IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetService<IGlobalConfiguration>();
            if (configuration == null)
            {
                throw new InvalidOperationException(
                    "Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddHangfire' inside the call to 'ConfigureServices(...)' in the application startup code.");
            }
        }
    }
}
