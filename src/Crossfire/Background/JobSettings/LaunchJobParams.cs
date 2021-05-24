// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Crossfire.Background.Common;
using Crossfire.Extensions;
using Crossfire.Extensions.CodeSecurity;
using Crossfire.Model.Query;
using Microsoft.Azure.Management.Analysis;
using Microsoft.Rest;

namespace Crossfire.Background.JobSettings
{
    /// <summary>
    /// Configuration for a <see cref="LaunchExecutorJob"/>.
    /// </summary>
    public sealed class LaunchJobParams : BackgroundJobParams
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LaunchJobParams"/> class.
        /// </summary>
        public LaunchJobParams()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LaunchJobParams"/> class.
        /// </summary>
        /// <param name="root">A base config to initialize from.</param>
        public LaunchJobParams(BackgroundJobParams root)
            : base()
        {
            this.UserPrincipalName = root.UserPrincipalName;
            this.UserSubscriberName = root.UserSubscriberName;
            this.ConnectedServers = root.ConnectedServers;
            this.KeyVaultUri = root.KeyVaultUri;
        }

        /// <summary>
        /// Retrieves a token to be used when authenticating in <see cref="LaunchExecutorJob"/>.
        /// </summary>
        /// <param name="request">Request describing a target server to launch <see cref="LaunchRequest"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<string> GetPrincipalToken(LaunchRequest request)
        {
            var server = this.ConnectedServers.FirstOrDefault(s => request.ServerQualifiedName == s.ServerQualifiedName);
            if (server == null)
            {
                throw new Exception($"Server {request.ServerQualifiedName} not found in connected servers");
            }

            var clientSecret = await IdentityManager.GetSecret(this.KeyVaultUri, server.ApplicationId, server.TenantId);

            return IdentityManager.SecureStringToString(await IdentityManager.GetServiceBearerToken(
                tenantId: server.TenantId,
                clientId: server.ApplicationId,
                clientSecret: clientSecret,
                serviceUri: "https://management.azure.com/"));
        }

        /// <summary>
        /// Returns a subscription identifier for a server specified in a request.
        /// </summary>
        /// <param name="request"><see cref="LaunchRequest"/>.</param>
        /// <returns>Azure subscription identifier.</returns>
        public string GetSubscriptionId(LaunchRequest request)
        {
            var server = this.ConnectedServers.FirstOrDefault(s => request.ServerQualifiedName == s.ServerQualifiedName);
            if (server == null)
            {
                throw new Exception($"Server {request.ServerQualifiedName} not found in connected servers");
            }

            return server.SubscriptionId;
        }

        /// <summary>
        /// Creates an instance of Azure AS management client.
        /// </summary>
        /// <param name="principalToken">Access token to be used when connecting to an Azure AS instance.</param>
        /// <param name="subscriptionId">Azure subscription identifier.</param>
        /// <returns>An instance of <see cref="AnalysisServicesManagementClient"/> using the provided token and subscription.</returns>
        public AnalysisServicesManagementClient CreateAnalysisServicesManagementClient(SecureString principalToken, string subscriptionId)
        {
            TokenCredentials creds = new TokenCredentials(token: IdentityManager.SecureStringToString(principalToken));

            return new AnalysisServicesManagementClient(creds) { SubscriptionId = subscriptionId };
        }

        /// <summary>
        /// Generates connected server hash to be used by a client connection that sent a request.
        /// </summary>
        /// <param name="request"><see cref="LaunchRequest"/>.</param>
        /// <returns>SHA256 hash of a server FQN and a SignalR connection id.</returns>
        public string GetSHA256(LaunchRequest request) => TextSecurity.ComputeHashString($"{request.ServerQualifiedName}.{request.UniqueClientIdentifier}");
    }
}
