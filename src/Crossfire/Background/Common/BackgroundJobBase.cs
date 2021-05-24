// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Crossfire.Config;
using Crossfire.Model.Base;
using Crossfire.SignalR.Hubs;
using Crossfire.SignalR.Messages;
using Hangfire;
using Hangfire.States;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Crossfire.Background.Common
{
    // for reference only
    // not used in this implementation
    // https://stackoverflow.com/questions/48393429/get-hub-context-in-signalr-core-from-within-another-object?noredirect=1&lq=1

    /// <summary>
    /// Base class for Hangfire jobs.
    /// </summary>
    public abstract class BackgroundJobBase
    {
        private readonly Random random;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobBase"/> class.
        /// </summary>
        public BackgroundJobBase()
        {
            this.random = new Random();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobBase"/> class.
        /// </summary>
        /// <param name="cache"><see cref="MemoryCache"/>.</param>
        /// <param name="loggerFactory">A factory to produce a <see cref="Logger"/>.</param>
        /// <param name="hubContext"><see cref="HubContext"/>.</param>
        public BackgroundJobBase(IMemoryCache cache, ILoggerFactory loggerFactory, IHubContext<ModelMessageHub> hubContext)
            : this()
        {
            this.MemoryCache = cache;
            this.Logger = loggerFactory.CreateLogger(nameof(BackgroundJobBase));
            this.HubContext = hubContext;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobBase"/> class.
        /// </summary>
        /// <param name="loggerFactory">A factory to produce a <see cref="Logger"/>.</param>
        /// <param name="hubContext"><see cref="HubContext"/>.</param>
        public BackgroundJobBase(ILoggerFactory loggerFactory, IHubContext<ModelMessageHub> hubContext)
            : this()
        {
            this.Logger = loggerFactory.CreateLogger(nameof(BackgroundJobBase));
            this.HubContext = hubContext;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobBase"/> class.
        /// </summary>
        /// <param name="hubContext"><see cref="HubContext"/>.</param>
        public BackgroundJobBase(IHubContext<ModelMessageHub> hubContext)
            : this()
        {
            this.HubContext = hubContext;
        }

        /// <summary>
        /// Gets an in-memory cache to be used by a job if needed.
        /// </summary>
        protected IMemoryCache MemoryCache { get; }

        /// <summary>
        /// Gets a job logger.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets signalR hub context.
        /// Allows a job to communicate with connected clients.
        /// </summary>
        protected IHubContext<ModelMessageHub> HubContext { get; }

        /// <summary>
        /// Adds a job to Hangire queue.
        /// </summary>
        /// <typeparam name="TRequest">Request type.</typeparam>
        /// <typeparam name="TParams">Job configuration type.</typeparam>
        /// <param name="request">Incoming request.</param>
        /// <param name="jobParams">Job configuration.</param>
        /// <param name="processorConfig">Hangfire worker configuration.</param>
        /// <returns>JobId if successful, null if failed.</returns>
        public virtual string AcceptForProcessing<TRequest, TParams>(TRequest request, TParams jobParams, QueryProcessorConfig processorConfig)
            where TRequest : JsonQueryRequest
            where TParams : BackgroundJobParams
        {
            var client = new BackgroundJobClient();
            var queue = processorConfig.GetQueues(System.Net.Dns.GetHostName()).OrderBy(a => this.random.NextDouble()).First();
            var state = new EnqueuedState(queue);
            return client.Create(() => this.Process(request, jobParams), state);
        }

        /// <summary>
        /// Job action.
        /// </summary>
        /// <typeparam name="T">Request type (query, metadata, etc.).</typeparam>
        /// <param name="request">Job input.</param>
        /// <param name="jobParams">Job configuration.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous job processing operation.</returns>
        public abstract Task Process<T>(T request, BackgroundJobParams jobParams)
            where T : JsonQueryRequest;

        /// <summary>
        /// Invokes SignalR method and sends query result to a subscribed client.
        /// </summary>
        /// <param name="queryMetadata">Query metadata sent by a client, must be returned back to it.</param>
        /// <param name="result">Query result.</param>
        /// <param name="userSubscriberName"><see cref="BackgroundJobParams.UserSubscriberName"/>.</param>
        /// <param name="uniqueClientIdentifier">SignalR connection identifier.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous push of <see cref="QueryResultMessage"/> from a the hub to a client.</returns>
        protected async Task SendResult(string queryMetadata, string result, string userSubscriberName, string uniqueClientIdentifier)
        {
            var message = new QueryResultMessage { QueryMetadata = queryMetadata, Payload = result, UserSubscriberName = userSubscriberName };
            await this.HubContext.Clients.Group(ModelMessageHub.GetChannel(ModelMessageHub.GetGroupIdentifier(userSubscriberName, uniqueClientIdentifier), HubChannels.QUERY_CHANNEL)).SendAsync(ModelMessageHub.QUERY_RESPONSE_MESSAGE, message);
        }

        /// <summary>
        /// Sends an error to a connected client.
        /// </summary>
        /// <param name="errorMessage">Error description.</param>
        /// <param name="userSubscriberName"><see cref="BackgroundJobParams.UserSubscriberName"/>.</param>
        /// <param name="uniqueClientIdentifier">SignalR connection identifier.</param>
        /// <param name="queryRequest">Request that failed to execute a job correctly.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous push to a connected client.</returns>
        protected async Task SendError(string errorMessage, string userSubscriberName, string uniqueClientIdentifier, JsonQueryRequest queryRequest)
        {
            var message = new ErrorMessage { Payload = errorMessage, UserSubscriberName = userSubscriberName, SubmittedRequest = queryRequest };
            await this.HubContext.Clients.Group(ModelMessageHub.GetChannel(ModelMessageHub.GetGroupIdentifier(userSubscriberName, uniqueClientIdentifier), HubChannels.ERROR_CHANNEL)).SendAsync(ModelMessageHub.ERROR_RESPONSE_MESSAGE, message);
        }

        /// <summary>
        /// Sends a connected server model's metadata to the client.
        /// </summary>
        /// <param name="result">Serialized model metadata.</param>
        /// <param name="userSubscriberName"><see cref="BackgroundJobParams.UserSubscriberName"/>.</param>
        /// <param name="uniqueClientIdentifier">SignalR connection identifier.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous push to a connected client.</returns>
        protected async Task SendMetadata(string result, string userSubscriberName, string uniqueClientIdentifier)
        {
            var message = new MetadataMessage { Payload = result, UserSubscriberName = userSubscriberName };
            await this.HubContext.Clients.Group(ModelMessageHub.GetChannel(ModelMessageHub.GetGroupIdentifier(userSubscriberName, uniqueClientIdentifier), HubChannels.METADATA_CHANNEL)).SendAsync(ModelMessageHub.METADATA_RESPONSE_MESSAGE, message);
        }

        /// <summary>
        /// Sends a heartbeat message, indicating current server health status to the connected client.
        /// </summary>
        /// <param name="serverQualifiedName">Fully qualified server name: region, resource group and server name.</param>
        /// <param name="heartbeatState">Current health state.</param>
        /// <param name="serverSHA">Unique string to identify a server-connected client relation.</param>
        /// <param name="uniqueClientIdentifier">SignalR connection identifier.</param>
        /// <param name="userSubscriberName"><see cref="BackgroundJobParams.UserSubscriberName"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous push to a connected client.</returns>
        protected async Task SendHeartbeat(string serverQualifiedName, string heartbeatState, string serverSHA, string uniqueClientIdentifier, string userSubscriberName)
        {
            var message = new HeartbeatMessage { Timestamp = DateTime.UtcNow, Server = serverQualifiedName, HeartbeatState = heartbeatState, ServerHash = serverSHA };
            await this.HubContext.Clients.Group(ModelMessageHub.GetChannel(ModelMessageHub.GetGroupIdentifier(userSubscriberName, uniqueClientIdentifier), HubChannels.HEARTBEAT_CHANNEL)).SendAsync(ModelMessageHub.HEARTBEAT_BROADCAST_MESSAGE, message);
        }

        /// <summary>
        /// SinglaR method to send job status.
        /// </summary>
        /// <param name="jobId">Reported job Id.</param>
        /// <param name="jobStatus">Reported job status.</param>
        /// <param name="userSubscriberName">User to receive the message.</param>
        /// <param name="uniqueClientIdentifier">SignalR connection identifier.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous push to a connected client.</returns>
        protected async Task SendStatus(string jobId, string jobStatus, string userSubscriberName, string uniqueClientIdentifier)
        {
            var message = new { Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), JobId = jobId, JobStatus = jobStatus };
            await this.HubContext.Clients.Group(ModelMessageHub.GetGroupIdentifier(userSubscriberName, uniqueClientIdentifier)).SendAsync(ModelMessageHub.JOBSTATUS_RESPONSE_MESSAGE, message);
        }

        /// <summary>
        /// Sends a list of servers that user has access to.
        /// </summary>
        /// <param name="result">Serialized list of servers.</param>
        /// <param name="userSubscriberName"><see cref="BackgroundJobParams.UserSubscriberName"/>.</param>
        /// <param name="uniqueClientIdentifier">SignalR connection identifier.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous push to a connected client.</returns>
        protected async Task SendConnectionInfo(string result, string userSubscriberName, string uniqueClientIdentifier)
        {
            var message = new UserConnectionInfoMessage { Payload = result, UserSubscriberName = userSubscriberName };
            string channel = ModelMessageHub.GetChannel(ModelMessageHub.GetGroupIdentifier(userSubscriberName, uniqueClientIdentifier), HubChannels.INFO_CHANNEL);
            await this.HubContext.Clients.Group(channel).SendAsync(ModelMessageHub.CONNECTIONINFO_RESPONSE_MESSAGE, message);
        }

        /// <summary>
        /// Default in-memory cache options.
        /// </summary>
        /// <param name="size">Cache entry size in bytes.</param>
        /// <returns><see cref="MemoryCacheEntryOptions"/>.</returns>
        protected virtual MemoryCacheEntryOptions CacheOptions(long size)
        {
            return new MemoryCacheEntryOptions()
            {
                Size = size,
            };
        }
    }
}
