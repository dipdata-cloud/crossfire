// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Crossfire.Model.Metadata
{
    /// <summary>
    /// Azure AS instance a user has access to.
    /// </summary>
    public class UserConnectionInfo
    {
        /// <summary>
        /// Gets or sets a server name.
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Gets or sets the database name.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Gets or sets the region name.
        /// </summary>
        public string AzureRegion { get; set; }

        /// <summary>
        /// Gets or sets the resource group name.
        /// </summary>
        public string AzureResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets the model name.
        /// </summary>
        public string Model { get; set; }
    }
}
