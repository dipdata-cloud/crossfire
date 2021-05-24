// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Crossfire.Background;
using Crossfire.Background.JobSettings;
using Crossfire.Extensions;
using Crossfire.Model.Query;
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
    /// Tests for <see cref="QueryJob"/>.
    /// </summary>
    public class QueryJobTest : SignalRTest
    {
        /// <summary>
        /// Test setup.
        /// </summary>
        /// <param name="logger">Mocked logger factory.</param>
        /// <param name="context">SignalR hub context.</param>
        /// <returns>An instance of <see cref="QueryJob"/>.</returns>
        public async Task<QueryJob> Setup(ILoggerFactory logger, IHubContext<ModelMessageHub> context)
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var storageContext = new StorageContext(this.TestConfiguration.StorageAccountConnectionString, null);
            await storageContext.CreateTable(nameof(QueryJobTest));

            var tokenCacheClient = new CachedEntityClient<string>(storageContext, nameof(QueryJobTest));

            return new QueryJob(memoryCache, logger, context, tokenCacheClient);
        }

        /// <summary>
        /// Simple test for Process method.
        /// NB: Not guaranteed to work when Azure AS instance is unresponsive.
        /// </summary>
        /// <returns>XUnit test task.</returns>
        [Fact]
        public async Task Process()
        {
            var (context, client) = this.SetupHubContext();
            var logger = new Mock<ILoggerFactory>();
            var clientId = Guid.NewGuid().ToString();
            var job = await this.Setup(logger.Object, context.Object);

            var request = new QueryRequest
            {
                TargetDatabase = "adventureworks",
                Region = this.TestConfiguration.AzureAS.Region,
                ResourceGroup = this.TestConfiguration.AzureAS.ResourceGroup,
                TargetServer = this.TestConfiguration.AzureAS.TargetServer,
                UniqueClientIdentifier = clientId,
                CompilationTarget = CompilationTargets.MDX,
                OutputFormat = 0,
                QueryValues = new string[] { "[Measures].[Internet Total Sales]" },
                QuerySlices = new string[] { "[Date].[Fiscal Year].[All].children" },
                RequestMetadata = "test.object",
            };
            var jobParams = new QueryJobParams
            {
                ConnectedServers = this.TestServers,
                UserSubscriberName = clientId,
                KeyVaultUri = this.TestConfiguration.KeyVault.Uri,
                UserPrincipalName = "dummy",
            };

            await job.Process(request, jobParams);

            client.Verify(
                clientProxy => clientProxy.SendCoreAsync(
                    ModelMessageHub.QUERY_RESPONSE_MESSAGE,
                    It.Is<object[]>(o => o != null && (o[0] as MetadataMessage) != null),
                    default(CancellationToken)),
                Times.Never());

            client.Verify(
                clientProxy => clientProxy.SendCoreAsync(
                    ModelMessageHub.ERROR_RESPONSE_MESSAGE,
                    It.Is<object[]>(o => o != null && (o[0] as ErrorMessage) != null && (o[0] as ErrorMessage).UserSubscriberName == clientId),
                    default(CancellationToken)),
                Times.Once());
        }
    }
}
