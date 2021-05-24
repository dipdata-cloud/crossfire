// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Crossfire.Background.Common;
using Crossfire.Background.JobSettings;
using Crossfire.Extensions;
using Crossfire.Model;
using Crossfire.Model.Metadata;
using Crossfire.SignalR.Hubs;
using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Crossfire.Background
{
    /// <summary>
    /// A job that retrieves model structure in a form of JSON document.
    /// </summary>
    public sealed class MetadataJob : BackgroundJobBase
    {
        private readonly CachedEntityClient<string> tokenCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataJob"/> class.
        /// </summary>
        /// <param name="cache">Memory cache to hold received metadata.</param>
        /// <param name="loggerFactory">Logger factory to produce a logger.</param>
        /// <param name="hubContext">SignalR hub context.</param>
        /// <param name="tokenCache">Distributed cache for backend engine access tokens.</param>
        public MetadataJob(IMemoryCache cache, ILoggerFactory loggerFactory, IHubContext<ModelMessageHub> hubContext, CachedEntityClient<string> tokenCache)
            : base(cache, loggerFactory, hubContext)
        {
            this.tokenCache = tokenCache;
        }

        /// <inheritdoc/>
        public override async Task Process<T>(T request, BackgroundJobParams jobParams)
        {
            JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented,
            };

            QueryJobParams metadataJobParams = new QueryJobParams(jobParams);
            var metadataRequest = request as ModelMetadataRequest;

            string cacheKey = $"{jobParams.UserPrincipalName}#{request.GetSHA256()}";
            try
            {
                if (!this.MemoryCache.TryGetValue(cacheKey, out ModelMetadata metadata))
                {
                    var token = await metadataJobParams.GetServicePrincipalToken(this.tokenCache, metadataRequest);

                    ModelConnection modelConnection = new ModelConnection(
                       $"Data Source=asazure://{request.Region}.asazure.windows.net/{request.TargetServer};User ID=;Password={token};Persist Security Info=True;Impersonation Level=Impersonate;Initial Catalog={request.TargetDatabase}");

                    metadata = await modelConnection.RetrieveMetadataAsync(request.TargetDatabase);
                    if (metadata != null)
                    {
                        this.MemoryCache.Set(cacheKey, metadata, this.CacheOptions());
                    }
                }

                await this.SendMetadata(JsonConvert.SerializeObject(metadata, serializerSettings), metadataJobParams.UserSubscriberName, request.UniqueClientIdentifier);
            }
            catch (Exception ex)
            {
                await this.SendError(ex.Message, metadataJobParams.UserSubscriberName, request.UniqueClientIdentifier, request);
                if (ex.GetType() == typeof(AdomdConnectionException))
                {
                    await this.SendHeartbeat(request.ServerQualifiedName, ModelServerState.Offline, string.Empty, request.UniqueClientIdentifier, metadataJobParams.UserSubscriberName);
                }
            }
        }

        /// <inheritdoc/>
        protected override MemoryCacheEntryOptions CacheOptions(long size = 128)
        {
            var baseOptions = base.CacheOptions(size);
            baseOptions.SlidingExpiration = TimeSpan.FromMinutes(30);
            baseOptions.Priority = CacheItemPriority.Normal;

            return baseOptions;
        }
    }
}
