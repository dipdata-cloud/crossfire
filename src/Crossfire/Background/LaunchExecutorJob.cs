// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Crossfire.Background.Common;
using Crossfire.Background.JobSettings;
using Crossfire.Extensions;
using Crossfire.Model.Query;
using Crossfire.SignalR.Hubs;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.Management.Analysis;
using Microsoft.Extensions.Logging;

namespace Crossfire.Background
{
    /// <summary>
    /// A Hangfire job that launches a specified Azure AS instance.
    /// </summary>
    public sealed class LaunchExecutorJob : BackgroundJobBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LaunchExecutorJob"/> class.
        /// </summary>
        /// <param name="loggerFactory">Logger factory.</param>
        /// <param name="hubContext">SignalR hub context.</param>
        public LaunchExecutorJob(ILoggerFactory loggerFactory, IHubContext<ModelMessageHub> hubContext)
            : base(loggerFactory, hubContext)
        {
        }

        // Only public methods can be invoked in background, see Hangfire docs

        /// <inheritdoc/>
        [AutomaticRetry(Attempts = 3)]
        public override async Task Process<T>(T request, BackgroundJobParams jobParams)
        {
            LaunchJobParams launchJobParams = new LaunchJobParams(jobParams);
            var launchRequest = request as LaunchRequest;
            string token = await launchJobParams.GetPrincipalToken(launchRequest);

            // refresh management client since token has probably expired
            AnalysisServicesManagementClient managementClient = launchJobParams.CreateAnalysisServicesManagementClient(
                principalToken: IdentityManager.StringToSecureString(token),
                subscriptionId: launchJobParams.GetSubscriptionId(launchRequest));

            var details = await managementClient.Servers.GetDetailsAsync(request.ResourceGroup, request.TargetServer);

            if (details.State.Equals("Paused", StringComparison.InvariantCultureIgnoreCase))
            {
                await this.SendHeartbeat(request.ServerQualifiedName, ModelServerState.Updating, string.Empty, request.UniqueClientIdentifier, launchJobParams.UserSubscriberName);
                await managementClient.Servers.ResumeAsync(request.ResourceGroup, request.TargetServer);
            }

            await this.SendHeartbeat(request.ServerQualifiedName, ModelServerState.Online, launchJobParams.GetSHA256(launchRequest), request.UniqueClientIdentifier, launchJobParams.UserSubscriberName);
        }
    }
}
