// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using Crossfire.Background.Common;
using Crossfire.Model.Base;
using Crossfire.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Crossfire.Background
{
    /// <summary>
    /// A job that retrieves another Hangfire job status.
    /// </summary>
    public sealed class StatusJob : BackgroundJobBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusJob"/> class.
        /// </summary>
        /// <param name="hubContext">SignalR hub context.</param>
        public StatusJob(IHubContext<ModelMessageHub> hubContext)
            : base(hubContext)
        {
        }

        /// <summary>
        /// Immediately sends job status to the connected client.
        /// </summary>
        /// <param name="jobRequest">An original job control request.</param>
        /// <param name="jobStatus">Resulting job status.</param>
        /// <param name="userSubscriberName"><see cref="BackgroundJobParams.UserSubscriberName"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous push of a job status to the connected client.</returns>
        public async Task AcceptStatusJob(JobRequest jobRequest, string jobStatus, string userSubscriberName)
        {
            await this.SendStatus(jobRequest.JobId, jobStatus, userSubscriberName, jobRequest.UniqueClientIdentifier);
        }

        /// <inheritdoc/>
        public override Task Process<T>(T request, BackgroundJobParams jobParams)
        {
            throw new System.NotImplementedException();
        }
    }
}
