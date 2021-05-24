// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Crossfire.Background;
using Crossfire.Background.JobSettings;
using Crossfire.Model.Query;
using Crossfire.SignalR.Hubs;
using Crossfire.SignalR.Messages;
using CrossfireXUnit.Local;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CrossfireXUnit.Jobs
{
    /// <summary>
    /// Tests for <see cref="LaunchExecutorJob"/>.
    /// </summary>
    public class LaunchExecutorJobTest : SignalRTest
    {
        /// <summary>
        /// Tests <see cref="LaunchExecutorJob.Process{T}(T, Crossfire.Background.Common.BackgroundJobParams)"/> method.
        /// NB: this is not guaranteed to provide 100% coverage with current implementation. Test might fail if Azure AS instance is unresponsive.
        /// </summary>
        /// <returns>XUnit test task.</returns>
        [Fact]
        public async Task Process()
        {
            var (context, client) = this.SetupHubContext();
            var logger = new Mock<ILoggerFactory>();
            var clientId = Guid.NewGuid().ToString();

            var job = new LaunchExecutorJob(logger.Object, context.Object);
            var request = new LaunchRequest
            {
                TargetDatabase = "adventureworks",
                Region = this.TestConfiguration.AzureAS.Region,
                ResourceGroup = this.TestConfiguration.AzureAS.ResourceGroup,
                TargetServer = this.TestConfiguration.AzureAS.TargetServer,
                UniqueClientIdentifier = clientId,
            };
            var jobParams = new LaunchJobParams
            {
                ConnectedServers = this.TestServers,
                UserSubscriberName = clientId,
                KeyVaultUri = this.TestConfiguration.KeyVault.Uri,
                UserPrincipalName = "test@{TestConfiguration.Tenant}",
            };

            await job.Process(request, jobParams);

            client.Verify(
                clientProxy => clientProxy.SendCoreAsync(
                    ModelMessageHub.HEARTBEAT_BROADCAST_MESSAGE,
                    It.Is<object[]>(o => o != null && (o[0] as HeartbeatMessage) != null),
                    default(CancellationToken)),
                Times.Once);
        }
    }
}
