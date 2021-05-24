// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace CrossfireXUnit.Local
{
    /// <summary>
    /// Structure of testsettings.json.
    /// </summary>
    public class TestConfiguration
    {
        /// <summary>
        /// Gets or sets Azure Storage connection string.
        /// Azurite or real account both work.
        /// </summary>
        public string StorageAccountConnectionString { get; set; }

        /// <summary>
        /// Gets or sets KeyVault configuration.
        /// </summary>
        public KeyVaultConfiguration KeyVault { get; set; }

        /// <summary>
        /// Gets or sets ID of a tenant tests will run against.
        /// </summary>
        public string Tenant { get; set; }

        /// <summary>
        /// Gets or sets ID of a client application used to access Graph API.
        /// </summary>
        public string AzureAdGraphApiClientId { get; set; }

        /// <summary>
        /// Gets or sets test AD user objectId.
        /// </summary>
        public string AzureAdGraphApiTestUserId { get; set; }

        /// <summary>
        /// Gets or sets test AD user UPN.
        /// </summary>
        public string AzureAdGraphApiTestUPN { get; set; }

        /// <summary>
        /// Gets or sets configuration for Azure AS tests.
        /// </summary>
        public AzureASConfiguration AzureAS { get; set; }
    }

    /// <summary>
    /// KeyVault test configuration.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Reviewed.")]
    public class KeyVaultConfiguration
    {
        /// <summary>
        /// Gets or sets a service URI.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets test secrets.
        /// </summary>
        public KeyVaultTestSecret[] TestSecrets { get; set; }
    }

    /// <summary>
    /// KeyVault test secret.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Reviewed.")]
    public class KeyVaultTestSecret
    {
        /// <summary>
        /// Gets or sets a test secret name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a test secret value.
        /// </summary>
        public string Secret { get; set; }
    }

    /// <summary>
    /// Azure AS test configuration.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Reviewed.")]
    public class AzureASConfiguration
    {
        /// <summary>
        /// Gets or sets an Azure AS endpoint (https://northeurope.asazure.windows.net).
        /// </summary>
        public string ServerUri { get; set; }

        /// <summary>
        /// Gets or sets a client application ID that is an Azure AS admin.
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// Gets or sets part of Azure AS connection string with Data Provider.
        /// </summary>
        public string ConnectionProvider { get; set; }

        /// <summary>
        /// Gets or sets Azure RG.
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets Azure Region.
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets Azure AS server name.
        /// </summary>
        public string TargetServer { get; set; }

        /// <summary>
        /// Gets or sets Azure AS URI with asazure:// protocol.
        /// </summary>
        public string ServerServiceUri { get; set; }

        /// <summary>
        /// Gets or sets a subscription id that runs Azure AS test server.
        /// </summary>
        public string SubscriptionId { get; set; }
    }
}
