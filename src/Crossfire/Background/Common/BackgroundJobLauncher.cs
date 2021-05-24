// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Crossfire.Config;
using Crossfire.Model.Base;
using Crossfire.SignalR.Hubs;
using Hangfire;
using Microsoft.AspNetCore.SignalR;

namespace Crossfire.Background.Common
{
    /// <summary>
    /// Proxy for Hangfire job queue control.
    /// </summary>
    public sealed class BackgroundJobLauncher
    {
        private readonly ConnectedServerConfig[] connectedServers;
        private readonly string keyVaultUri;
        private readonly QueryProcessorConfig queryProcessorConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobLauncher"/> class.
        /// </summary>
        /// <param name="connectedServers">Connected Azure AS instances.</param>
        /// <param name="keyVaultUri">URI of Azure KeyVault to be used by all jobs.</param>
        /// <param name="queryProcessorConfig">Hangfire worker configuration.</param>
        public BackgroundJobLauncher(ConnectedServerConfig[] connectedServers, string keyVaultUri, QueryProcessorConfig queryProcessorConfig)
        {
            this.connectedServers = connectedServers;
            this.keyVaultUri = keyVaultUri;
            this.queryProcessorConfig = queryProcessorConfig;
        }

        /// <summary>
        /// Accepts a job for background processing.
        /// </summary>
        /// <typeparam name="TJob">Job type.</typeparam>
        /// <typeparam name="TRequest">Request type.</typeparam>
        /// <param name="job">A job to execute.</param>
        /// <param name="request">A request to process in the job.</param>
        /// <param name="userPrincipalName"><see cref="BackgroundJobParams.UserPrincipalName"/>.</param>
        /// <param name="userSubscriberName"><see cref="BackgroundJobParams.UserSubscriberName"/>.<</param>
        /// <returns>Job identifier.</returns>
        public string AcceptJob<TJob, TRequest>(TJob job, TRequest request, string userPrincipalName, string userSubscriberName)
            where TJob : BackgroundJobBase
            where TRequest : JsonQueryRequest
        {
            BackgroundJobParams jobParams = new BackgroundJobParams()
            {
                UserPrincipalName = userPrincipalName,
                UserSubscriberName = userSubscriberName,
                ConnectedServers = this.connectedServers,
                KeyVaultUri = this.keyVaultUri,
            };

            return job.AcceptForProcessing(request, jobParams, this.queryProcessorConfig);
        }

        /// <summary>
        /// Cancels a Hangfire job and removes it from the queue.
        /// </summary>
        /// <param name="request">Job control request.</param>
        /// <param name="hubContext">SignalR hub context.</param>
        /// <param name="userSubscriberName"><see cref="BackgroundJobParams.UserSubscriberName"/>.</param>
        /// <returns>Asynchronous job cancel and a report back to client from SignalR hub.</returns>
        [ExcludeFromCodeCoverage]
        public async Task CancelJob(JobRequest request, IHubContext<ModelMessageHub> hubContext, string userSubscriberName)
        {
            bool cancellationResult = BackgroundJob.Delete(request.JobId);
            StatusJob job = new StatusJob(hubContext: hubContext);
            if (cancellationResult)
            {
                await job.AcceptStatusJob(request, JobRequest.JOBACTION_CANCEL, userSubscriberName);
            }
            else
            {
                await job.AcceptStatusJob(request, JobRequest.JOBACTION_FAILURE, userSubscriberName);
            }
        }

        /// <summary>
        /// Requeues a Hangfire job.
        /// </summary>
        /// <param name="request">Job control request.</param>
        /// <param name="hubContext">SignalR hub context.</param>
        /// <param name="userSubscriberName"><see cref="BackgroundJobParams.UserSubscriberName"/>.</param>
        /// <returns>Asynchronous job requeue and a report back to client from SignalR hub.</returns>
        [ExcludeFromCodeCoverage]
        public async Task RequeueJob(JobRequest request, IHubContext<ModelMessageHub> hubContext, string userSubscriberName)
        {
            bool requeueResult = BackgroundJob.Requeue(request.JobId);
            StatusJob job = new StatusJob(hubContext: hubContext);
            if (requeueResult)
            {
                await job.AcceptStatusJob(request, JobRequest.JOBACTION_REQUEUE, userSubscriberName);
            }
            else
            {
                await job.AcceptStatusJob(request, JobRequest.JOBACTION_FAILURE, userSubscriberName);
            }
        }
    }
}
