// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Crossfire.Background.Common;
using Crossfire.Background.JobSettings;
using Crossfire.Config;
using Crossfire.Extensions;
using Crossfire.Model;
using Crossfire.Model.Metadata;
using Crossfire.SignalR.Hubs;
using Microsoft.AnalysisServices.Tabular;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.Management.Analysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Crossfire.Background
{
    /// <summary>
    /// A Hangfire job that retrieves information about Azure AS models a user has access to.
    /// </summary>
    public sealed class UserConnectionInfoJob : BackgroundJobBase
    {
        private readonly CachedEntityClient<UserConnectionInfo[]> resultCache;
        private readonly CachedEntityClient<string> tokenCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserConnectionInfoJob"/> class.
        /// </summary>
        /// <param name="cache">Memory cache to hold a job result.</param>
        /// <param name="loggerFactory">Factory to initialize a logger.</param>
        /// <param name="hubContext">SignalR hub context.</param>
        /// <param name="resultCache">Distributed cache for job results so multiple servers can reuse it.</param>
        /// <param name="tokenCache">Azure AS token cache.</param>
        public UserConnectionInfoJob(IMemoryCache cache, ILoggerFactory loggerFactory, IHubContext<ModelMessageHub> hubContext, CachedEntityClient<UserConnectionInfo[]> resultCache, CachedEntityClient<string> tokenCache)
            : base(cache, loggerFactory, hubContext)
        {
            this.resultCache = resultCache;
            this.tokenCache = tokenCache;
        }

        /// <inheritdoc/>
        public override async Task Process<T>(T request, BackgroundJobParams jobParams)
        {
            UserConnectionInfoRequest userConnectionInfoRequest = request as UserConnectionInfoRequest;
            UserConnectionInfoJobParams userConnectionInfoJobParams = new UserConnectionInfoJobParams(jobParams);
            var result = await userConnectionInfoJobParams.GetCachedInfo(this.resultCache);
            var serverInfo = userConnectionInfoRequest.SearchServers.Length > 0
                                 ? userConnectionInfoJobParams.ConnectedServers.Where(srv => userConnectionInfoRequest.SearchServers.Contains(srv.ServerQualifiedName)).ToList()
                                 : userConnectionInfoJobParams.ConnectedServers.ToList();

            if (result.Length == 0)
            {
                result = await this.GetUserConnectionInfo(
                    userPrincipalName: jobParams.UserPrincipalName,
                    serverInfo: serverInfo,  // limit search only to servers provided in the request to avoid launching instances customer has no access to
                    clientRequestId: userConnectionInfoRequest.UniqueClientIdentifier,
                    userSubscriberName: jobParams.UserSubscriberName);

                // avoid caching empty results
                if (result.Length > 0)
                {
                    await this.resultCache.Set(userConnectionInfoJobParams.UserPrincipalName, request.UniqueClientIdentifier, result, 30 * 24 * 60 * 60);
                }
            }
            else
            {
                await this.LaunchUserExecutors(
                    serverInfo: serverInfo,
                    clientRequestId: userConnectionInfoRequest.UniqueClientIdentifier,
                    userSubscriberName: jobParams.UserSubscriberName);
            }

            JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented,
            };
            if (result.Length > 0)
            {
                await this.SendConnectionInfo(JsonConvert.SerializeObject(result, serializerSettings), userConnectionInfoJobParams.UserSubscriberName, request.UniqueClientIdentifier);
            }
            else
            {
                await this.SendError("No connection information found! Session probably expired - try logging in again", userConnectionInfoJobParams.UserSubscriberName, request.UniqueClientIdentifier, request);
            }
        }

        /// <inheritdoc/>
        protected override MemoryCacheEntryOptions CacheOptions(long size = 1)
        {
            var baseOptions = base.CacheOptions(size);
            baseOptions.SlidingExpiration = TimeSpan.FromHours(24);
            baseOptions.Priority = CacheItemPriority.High;

            return baseOptions;
        }

        private static bool HasMemberInRole(ModelRoleCollection roles, string userName)
        {
            return roles.Any(role =>
                role.ModelPermission == ModelPermission.Read && role.Members.Any(member => member.MemberName == userName));
        }

        private async Task<UserConnectionInfo[]> GetUserConnectionInfo(string userPrincipalName, List<ConnectedServerConfig> serverInfo, string clientRequestId, string userSubscriberName)
        {
            IEnumerable<Task<List<UserConnectionInfo>>> getInfoTasks = serverInfo.Select(async (server) =>
            {
                using var serverClient = new Server();
                return await this.TryAccess(
                serverClient: serverClient,
                server: server,
                userPrincipalName: userPrincipalName,
                clientRequestId: clientRequestId,
                userSubscriberName: userSubscriberName);
            });
            IEnumerable<List<UserConnectionInfo>> infos = await Task.WhenAll(getInfoTasks);
            return infos.SelectMany(_ => _).ToArray();
        }

        private async Task LaunchUserExecutors(List<ConnectedServerConfig> serverInfo, string clientRequestId, string userSubscriberName)
        {
            IEnumerable<Task> launchTasks = serverInfo.Select(async (server) =>
            {
                await this.LaunchExecutor(server, clientRequestId, userSubscriberName);
            });

            await Task.WhenAll(launchTasks);
        }

        private async Task LaunchExecutor(ConnectedServerConfig server, string clientRequestId, string userSubscriberName)
        {
            using AnalysisServicesManagementClient asManagementClient = await IdentityManager.CreateAnalysisServicesManagementClient(
                subscriptionId: server.SubscriptionId,
                tenantId: server.TenantId,
                tokenCache: this.tokenCache);
            var details = await asManagementClient.Servers.GetDetailsAsync(server.ResourceGroup, server.ServerName);

            if (details.State.Equals("Paused", StringComparison.InvariantCultureIgnoreCase))
            {
                await this.SendHeartbeat(server.ServerQualifiedName, ModelServerState.Updating, string.Empty, clientRequestId, userSubscriberName);
                await asManagementClient.Servers.ResumeAsync(server.ResourceGroup, server.ServerName);
            }
        }

        private async Task<List<UserConnectionInfo>> TryAccess(Server serverClient, ConnectedServerConfig server, string userPrincipalName, string clientRequestId, string userSubscriberName)
        {
            string token = await IdentityManager.GetMSIToken(server.ServerUri, server.TenantId);
            await this.LaunchExecutor(
                server: server,
                clientRequestId: clientRequestId,
                userSubscriberName: userSubscriberName);

            try
            {
                serverClient.Connect(this.GetConnectionString(uri: server.ServerServiceUri, token: token));

                List<UserConnectionInfo> result = serverClient.Databases.Cast<Database>()
                            .Where(d => d.Model != null && HasMemberInRole(d.Model.Roles, userPrincipalName))
                            .Select(db => new UserConnectionInfo
                            {
                                Server = server.ServerName,
                                Database = db.Name,
                                AzureRegion = server.Region,
                                AzureResourceGroup = server.ResourceGroup,
                                Model = db.Model.Name,
                            }).ToList();
                serverClient.Disconnect();

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.GetType()}: {ex.Message}");
                serverClient.Disconnect();
                return new List<UserConnectionInfo>();
            }
        }

        private string GetConnectionString(string uri, string token) => $"Data Source={uri};User ID=;Password={token}";
    }
}
