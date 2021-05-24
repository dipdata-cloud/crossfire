// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Crossfire.Background;
using Crossfire.Background.JobSettings;
using Crossfire.Extensions;
using Crossfire.Model.Metadata;
using Crossfire.SignalR.Hubs;
using Crossfire.SignalR.Messages;
using Crossfire.Storage;
using CrossfireXUnit.Local;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CrossfireXUnit.Jobs
{
    /// <summary>
    /// Tests <see cref="MetadataJob"/>.
    /// </summary>
    public class MetadataJobTest : SignalRTest
    {
        /// <summary>
        /// Test setup.
        /// </summary>
        /// <param name="logger">Mocked logger factory.</param>
        /// <param name="context">Mocked hub context.</param>
        /// <returns>An instance of <see cref="MetadataJob"/>.</returns>
        public async Task<MetadataJob> Setup(ILoggerFactory logger, IHubContext<ModelMessageHub> context)
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var storageContext = new StorageContext(this.TestConfiguration.StorageAccountConnectionString, null);
            await storageContext.CreateTable(nameof(MetadataJobTest));

            var tokenCacheClient = new CachedEntityClient<string>(storageContext, nameof(MetadataJobTest));

            return new MetadataJob(memoryCache, logger, context, tokenCacheClient);
        }

        /// <summary>
        /// Tests <see cref="MetadataJob.Process{T}(T, Crossfire.Background.Common.BackgroundJobParams)"/> method.
        /// NB: this is not guaranteed to provide 100% coverage with current implementation. Test might fail if Azure AS instance is unresponsive.
        /// </summary>
        /// <returns>XUnit test task.</returns>
        [Fact]
        public async Task Process()
        {
            var (context, client) = this.SetupHubContext();
            var logger = new Mock<ILoggerFactory>();
            var clientId = Guid.NewGuid().ToString();
            var job = await this.Setup(logger.Object, context.Object);

            var request = new ModelMetadataRequest
            {
                TargetDatabase = "adventureworks",
                Region = this.TestConfiguration.AzureAS.Region,
                ResourceGroup = this.TestConfiguration.AzureAS.ResourceGroup,
                TargetServer = this.TestConfiguration.AzureAS.TargetServer,
                UniqueClientIdentifier = clientId,
            };
            var jobParams = new QueryJobParams
            {
                ConnectedServers = this.TestServers,
                UserSubscriberName = clientId,
                KeyVaultUri = this.TestConfiguration.KeyVault.Uri,
                UserPrincipalName = "test@{TestConfiguration.Tenant}",
            };

            await job.Process(request, jobParams);

            client.Verify(
                clientProxy => clientProxy.SendCoreAsync(
                    ModelMessageHub.METADATA_RESPONSE_MESSAGE,
                    It.Is<object[]>(o => o != null && (o[0] as MetadataMessage) != null && (o[0] as MetadataMessage).UserSubscriberName == clientId),
                    default(CancellationToken)),
                Times.Once());
        }
    }
}
