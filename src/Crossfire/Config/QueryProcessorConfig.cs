// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq;

namespace Crossfire.Config
{
    /// <summary>
    /// Hangfire worker configuration.
    /// </summary>
    public sealed class QueryProcessorConfig
    {
        /// <summary>
        /// Gets or sets number of workers for each server.
        /// </summary>
        public int HangfireWorkers { get; set; }

        /// <summary>
        /// Gets or sets max cache size in bytes.
        /// </summary>
        public long CacheMaxSize { get; set; }

        /// <summary>
        /// Generates a list of queue names that are scoped to a host name.
        /// </summary>
        /// <param name="instanceName">A machine hostname.</param>
        /// <returns>A list of queues to be used by a Hangire server.</returns>
        public string[] GetQueues(string instanceName)
        {
            return Enumerable.Range(0, this.HangfireWorkers / 2 < 1 ? 1 : this.HangfireWorkers / 2)
                .Select(workerId => $"queue-for-{instanceName}-{workerId}".ToLowerInvariant())
                .ToArray();
        }
    }
}
