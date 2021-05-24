// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Crossfire.Config
{
    /// <summary>
    /// Configuration parameters for connected services.
    /// </summary>
    public sealed class AuthenticationConfig
    {
        /// <summary>
        /// Gets or sets a name of a secret containing storage account connection string.
        /// </summary>
        public string Storage { get; set; }

        /// <summary>
        /// Gets or sets Azure AD tenant identifier.
        /// </summary>
        public string AzureAdTenant { get; set; }

        /// <summary>
        /// Gets or sets clientId of an application used to access resources in the cloud.
        /// </summary>
        public string AzureAdClientId { get; set; }

        /// <summary>
        /// Gets or sets Azure AD policy that should be used to sign in a user.
        /// </summary>
        public string AzureAdPolicy { get; set; }

        /// <summary>
        /// Gets or sets tenant identifier of Azure Graph API endpoint.
        /// </summary>
        public string AzureGraphApiTenant { get; set; }

        /// <summary>
        /// Gets or sets clientId of an application used to interact with Graph API.
        /// </summary>
        public string AzureGraphClientId { get; set; }

        /// <summary>
        /// Gets or sets secret storage URI. Azure KeyVault is the only one supported as of now.
        /// </summary>
        public string KeyVaultUri { get; set; }

        /// <summary>
        /// Gets or sets allowed CORS origins.
        /// </summary>
        public string AllowedOrigin { get; set; }

        /// <summary>
        /// Gets or sets KeyVault secret that contains Azure SignalR connection key.
        /// </summary>
        public string AzureSignalRKey { get; set; }

        /// <summary>
        /// Gets or sets Azure SignalR address.
        /// </summary>
        public string AzureSignalREndpoint { get; set; }

        /// <summary>
        /// Gets or sets base URI for Jwt-based auth.
        /// </summary>
        public string JwtAuthorityBase { get; set; }
    }
}
