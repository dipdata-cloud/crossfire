// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;

namespace Crossfire.Model.Base
{
    /// <summary>
    /// Base class for all requests targeting Azure AS Models.
    /// </summary>
    public abstract class JsonQueryRequest
    {
        /// <summary>
        /// Gets or sets a unique client SignalR connection identifier issues by JoinServiceChannel method.
        /// </summary>
        public string UniqueClientIdentifier { get; set; }

        /// <summary>
        /// Gets or sets Azure region of a target Azure AS instance.
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets a target Azure AS database.
        /// </summary>
        public string TargetDatabase { get; set; }

        /// <summary>
        /// Gets or sets a target Azure AS server.
        /// </summary>
        public string TargetServer { get; set; }

        /// <summary>
        /// Gets or sets a target Azure AS Resource Group.
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets unique server hash.
        /// </summary>
        public string JobExecutorSHA { get; set; }

        /// <summary>
        /// Gets or sets any additional information a client wants to communicate to SignalR hub.
        /// </summary>
        public string RequestMetadata { get; set; }

        /// <summary>
        /// Gets fully qualified server name for a client.
        /// </summary>
        [JsonIgnore]
        public string ServerQualifiedName => this.TargetServer != null ? $"{this.Region}.{this.ResourceGroup}.{this.TargetServer}".ToLowerInvariant() : null;

        /// <summary>
        /// Request hash for in-memory cache.
        /// </summary>
        /// <returns>A SHA256 hash that matches this request data only.</returns>
        public abstract string GetSHA256();
    }
}
