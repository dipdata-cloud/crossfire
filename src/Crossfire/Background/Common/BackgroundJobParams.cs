// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Crossfire.Config;

namespace Crossfire.Background.Common
{
    /// <summary>
    /// Generic background job configuration.
    /// </summary>
    public class BackgroundJobParams
    {
        /// <summary>
        /// Gets or sets User Principal Name in Azure Active Directory to be used by a job.
        /// </summary>
        public string UserPrincipalName { get; set; }

        /// <summary>
        /// Gets or sets User object identifier in Azure Active Directory to be used by a job.
        /// </summary>
        public string UserSubscriberName { get; set; }

        /// <summary>
        /// Gets or sets a list of connected Azure AS instances.
        /// </summary>
        public ConnectedServerConfig[] ConnectedServers { get; set; }

        /// <summary>
        /// Gets or sets a secret storage URI for a background job. Azure KeyVault is the only one supported for now.
        /// </summary>
        public string KeyVaultUri { get; set; }
    }
}
