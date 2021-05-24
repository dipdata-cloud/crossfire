// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Crossfire.Config
{
    /// <summary>
    /// Configuration of a managed Azure AS instance.
    /// </summary>
    public sealed class ConnectedServerConfig
    {
        /// <summary>
        /// Gets or sets an Azure AS gateway, in a form of https://some-azure-region.asazure.windows.net.
        /// </summary>
        public string ServerUri { get; set; }

        /// <summary>
        /// Gets or sets an Azure AS server URI, in a form of asazure://some-azure-region.asazure.windows.net/server-name.
        /// </summary>
        public string ServerServiceUri { get; set; }

        /// <summary>
        /// Gets or sets an Azure region where server sits.
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets an Azure Resource Group where server resource sits.
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets an Azure AS server name.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Gets or sets the ApplicationId of a client application in Azure AD that is an Azure AS server admin.
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// Gets or sets the hosting Azure AD Tenant.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the hosting Azure Subscription identifier.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets a fully qualified server name to represent the server on a client level.
        /// </summary>
        public string ServerQualifiedName => $"{this.Region}.{this.ResourceGroup}.{this.ServerName}".ToLowerInvariant();
    }
}
