// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Crossfire.Config
{
    /// <summary>
    /// Configuration for a distributed cache that uses Azure Table API.
    /// </summary>
    public sealed class DistributedCacheConfig
    {
        /// <summary>
        /// Gets or sets the Azure Table name to be used by the token cache.
        /// </summary>
        public string TokenStore { get; set; }

        /// <summary>
        /// Gets or sets the Azure Table name to be used by the user connection info cache.
        /// </summary>
        public string ConnectionInfoStore { get; set; }
    }
}
