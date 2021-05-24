// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Crossfire.Background;
using Crossfire.Background.Common;
using Crossfire.Config;
using Crossfire.Model.Metadata;
using Crossfire.Model.Query;
using Crossfire.SignalR.Hubs;
using CrossfireXUnit.Local;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CrossfireXUnit.Jobs
{
    /// <summary>
    /// Tests for <see cref="BackgroundJobLauncher"/>.
    /// </summary>
    public class BackgroundJobLauncherTest : ParametizedTest
    {
        /// <summary>
        /// A few tests the <see cref="BackgroundJobLauncher.AcceptJob{TJob, TRequest}(TJob, TRequest, string, string)"/>.
        /// </summary>
        /// <param name="jobClass">Job type name to test against.</param>
        [Theory]
        [InlineData(nameof(LaunchExecutorJob))]
        [InlineData(nameof(MetadataJob))]
        public void AcceptJob(string jobClass)
        {
            var dummyConf = new ConnectedServerConfig
            {
                Region = string.Empty,
                ResourceGroup = string.Empty,
                ServerName = string.Empty,
            };
            var queryConf = new QueryProcessorConfig { CacheMaxSize = 1000L, HangfireWorkers = 1 };
            var launcher = new BackgroundJobLauncher(new ConnectedServerConfig[] { dummyConf }, this.TestConfiguration.KeyVault.Uri, queryConf);
            JobStorage.Current = new InMemoryStorage(new InMemoryStorageOptions() { DisableJobSerialization = false });
            var mockLog = new Mock<ILoggerFactory>();
            var mockHubContext = new Mock<IHubContext<ModelMessageHub>>();
            var mockCache = new Mock<IMemoryCache>();

            switch (jobClass)
            {
                case nameof(LaunchExecutorJob):
                    {
                        var job = new LaunchExecutorJob(mockLog.Object, mockHubContext.Object);
                        var request = new LaunchRequest
                        {
                            TargetDatabase = "test",
                            ResourceGroup = "test",
                            TargetServer = "test",
                            UniqueClientIdentifier = Guid.NewGuid().ToString(),
                        };
                        string jobId = launcher.AcceptJob(job, request, "test", "test1");
                        Assert.NotNull(jobId);
                        break;
                    }

                case nameof(MetadataJob):
                    {
                        var job = new MetadataJob(mockCache.Object, mockLog.Object, mockHubContext.Object, null);
                        var request = new ModelMetadataRequest
                        {
                            TargetDatabase = "test",
                            ResourceGroup = "test",
                            TargetServer = "test",
                            UniqueClientIdentifier = Guid.NewGuid().ToString(),
                        };
                        break;
                    }

                default:
                    Assert.True(false, $"Test case for non-existent job class: {jobClass}");
                    break;
            }
        }
    }
}
