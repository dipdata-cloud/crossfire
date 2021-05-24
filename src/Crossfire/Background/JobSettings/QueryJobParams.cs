// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Crossfire.Background.Common;
using Crossfire.Extensions;
using Crossfire.Model.Base;

namespace Crossfire.Background.JobSettings
{
    /// <summary>
    /// <see cref="QueryJob"/> job configuration.
    /// </summary>
    public sealed class QueryJobParams : BackgroundJobParams
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryJobParams"/> class.
        /// </summary>
        public QueryJobParams()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryJobParams"/> class.
        /// </summary>
        /// <param name="root">Configuration to initialize from.</param>
        public QueryJobParams(BackgroundJobParams root)
            : base()
        {
            this.UserPrincipalName = root.UserPrincipalName;
            this.UserSubscriberName = root.UserSubscriberName;
            this.ConnectedServers = root.ConnectedServers;
            this.KeyVaultUri = root.KeyVaultUri;
        }

        /// <summary>
        /// Retrieves SPN token to be used when authenticating to Azure AS.
        /// </summary>
        /// <param name="tokenCache">Distributed token cache.</param>
        /// <param name="request">Submitted request.</param>
        /// <returns>Token value (JWT Bearer Access Token).</returns>
        public async Task<string> GetServicePrincipalToken(CachedEntityClient<string> tokenCache, JsonQueryRequest request)
        {
            var server = this.ConnectedServers.FirstOrDefault(s => request.ServerQualifiedName == s.ServerQualifiedName);
            if (server == null)
            {
                throw new Exception($"Server {request.ServerQualifiedName} not found in connected servers");
            }

            var cachedToken = await tokenCache.Get(server.ServerQualifiedName);
            if (cachedToken == null)
            {
                var clientSecret = await IdentityManager.GetSecret(this.KeyVaultUri, server.ApplicationId, server.TenantId);
                var token = IdentityManager.SecureStringToString(await IdentityManager.GetServiceBearerToken(
                    tenantId: server.TenantId,
                    clientId: server.ApplicationId,
                    clientSecret: clientSecret,
                    serviceUri: server.ServerUri));

                await tokenCache.Set(server.ServerQualifiedName, request.UniqueClientIdentifier, token);

                return token;
            }
            else
            {
                return cachedToken.CachedValue;
            }
        }
    }
}
