// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Crossfire.Background.Common;
using Crossfire.Background.JobSettings;
using Crossfire.Extensions;
using Crossfire.Model;
using Crossfire.Model.Query;
using Crossfire.SignalR.Hubs;
using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Crossfire.Background
{
    public sealed class QueryJob : BackgroundJobBase
    {
        private readonly CachedEntityClient<string> tokenCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryJob"/> class.
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="hubContext"></param>
        /// <param name="tokenCache"></param>
        public QueryJob(IMemoryCache cache, ILoggerFactory loggerFactory, IHubContext<ModelMessageHub> hubContext, CachedEntityClient<string> tokenCache)
            : base(cache, loggerFactory, hubContext)
        {
            this.tokenCache = tokenCache;
        }

        /// <inheritdoc/>
        public override async Task Process<T>(T request, BackgroundJobParams jobParams)
        {
            QueryJobParams queryJobParams = new QueryJobParams(jobParams);
            QueryRequest queryRequest = request as QueryRequest;

            string cacheKey = $"{jobParams.UserPrincipalName}#{request.GetSHA256()}#{queryRequest.OutputFormat}";
            try
            {
                // execute query
                // always try to refresh empty result
                if (!this.MemoryCache.TryGetValue(cacheKey, out string result))
                {
                    var token = await queryJobParams.GetServicePrincipalToken(this.tokenCache, queryRequest);

                    var format = (ModelConnection.OutputFormat)queryRequest.OutputFormat;
                    ModelConnection mc = new ModelConnection(
                        $"Data Source=asazure://{request.Region}.asazure.windows.net/{request.TargetServer};User ID=;Password={token};Persist Security Info=True;Impersonation Level=Impersonate;Initial Catalog={request.TargetDatabase};EffectiveUserName={queryJobParams.UserPrincipalName}",
                        format);

                    result = await mc.ExecuteQueryAsync(queryRequest.Compile());

                    // avoid caching empty results
                    if (!string.IsNullOrEmpty(result) && result != ModelConnection.EMPTY_RESPONSE)
                    {
                        this.MemoryCache.Set(cacheKey, result, this.CacheOptions((result.Length / 100) + 1));
                    }
                }

                await this.SendResult(request.RequestMetadata, result, jobParams.UserSubscriberName, request.UniqueClientIdentifier);
            }
            catch (Exception ex)
            {
                await this.SendError(ex.Message, jobParams.UserSubscriberName, request.UniqueClientIdentifier, request);
                if (ex.GetType() == typeof(AdomdConnectionException))
                {
                    await this.SendHeartbeat(request.ServerQualifiedName, ModelServerState.Offline, string.Empty, request.UniqueClientIdentifier, queryJobParams.UserPrincipalName);
                }
            }
        }

        /// <inheritdoc/>
        protected override MemoryCacheEntryOptions CacheOptions(long size)
        {
            var baseOptions = base.CacheOptions(size);
            baseOptions.SlidingExpiration = TimeSpan.FromSeconds(300);
            baseOptions.Priority = CacheItemPriority.Low;

            return baseOptions;
        }
    }
}
