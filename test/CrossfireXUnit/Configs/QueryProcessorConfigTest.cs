// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Crossfire.Config;
using Xunit;

namespace CrossfireXUnit.Configs
{
    /// <summary>
    /// Tests for a Hangfire worker config.
    /// </summary>
    public class QueryProcessorConfigTest
    {
        /// <summary>
        /// Test cases for GetQueues.
        /// </summary>
        public static readonly List<object[]> QueryProcessorConfigTestCases = new List<object[]>
        {
            new object[] { new QueryProcessorConfig { CacheMaxSize = 1000L, HangfireWorkers = 7 }, "testABC", new string[] { "queue-for-testabc-0", "queue-for-testabc-1", "queue-for-testabc-2" } },
            new object[] { new QueryProcessorConfig { CacheMaxSize = 1000L, HangfireWorkers = 1 }, "testABC", new string[] { "queue-for-testabc-0" } },
        };

        /// <summary>
        /// Tests GetQueues of <see cref="QueryProcessorConfig"/>.
        /// </summary>
        /// <param name="config">Source config.</param>
        /// <param name="instanceName">Machine name (dummy).</param>
        /// <param name="expectedQueues">Expected queue list to be generated.</param>
        [Theory]
        [MemberData(nameof(QueryProcessorConfigTestCases))]
        public void GetQueues(QueryProcessorConfig config, string instanceName, string[] expectedQueues)
        {
            var actualQueues = config.GetQueues(instanceName);
            Assert.Equal(expectedQueues, actualQueues);
        }
    }
}
