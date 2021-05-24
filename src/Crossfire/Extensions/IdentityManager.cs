// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Management.Analysis;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;

namespace Crossfire.Extensions
{
    /// <summary>
    /// Obtains access tokens from Azure AD and secret values from KeyVault.
    /// </summary>
    public class IdentityManager
    {
        /// <summary>
        /// Connects to Azure KV using MSI and extracts application secret.
        /// </summary>
        /// <param name="keyVaultUri">Azure KeyVault URI.</param>
        /// <param name="secretId">Secret name.</param>
        /// <param name="tenantId">Azure AD tenant identifier.</param>
        /// <returns>Secret value as <see cref="SecureString"/>.</returns>
        public static async Task<SecureString> GetSecret(string keyVaultUri, string secretId, string tenantId)
        {
            var kv = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential(new DefaultAzureCredentialOptions() { SharedTokenCacheTenantId = tenantId, VisualStudioTenantId = tenantId, ExcludeSharedTokenCacheCredential = true }));
            return StringToSecureString((await kv.GetSecretAsync(secretId)).Value.Value);
        }

        /// <summary>
        /// Obtains an MSI token.
        /// </summary>
        /// <param name="serviceUri">Service uri to obtain a token for.</param>
        /// <param name="tenantId">Azure AD tenant identifier.</param>
        /// <returns>MSI token as string.</returns>
        public static async Task<string> GetMSIToken(string serviceUri, string tenantId = null)
        {
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            return await azureServiceTokenProvider.GetAccessTokenAsync(serviceUri, tenantId).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates an instance of Azure AS management client.
        /// </summary>
        /// <param name="subscriptionId">Azure Subscription identifier which hosts Azure AS server.</param>
        /// <param name="tenantId">Azure AD tenant identifier.</param>
        /// <param name="tokenCache">Access token cache.</param>
        /// <returns>An instance of <see cref="AnalysisServicesManagementClient"/>.</returns>
        public static async Task<AnalysisServicesManagementClient> CreateAnalysisServicesManagementClient(string subscriptionId, string tenantId, CachedEntityClient<string> tokenCache = null)
        {
            string token;

            // bypass cache in case it is not specified
            if (tokenCache == null)
            {
                token = await GetMSIToken("https://management.azure.com/", tenantId);
            }
            else
            {
                var cachedEntity = await tokenCache.Get($"management.azure.com.{tenantId}");
                if (cachedEntity == null)
                {
                    token = await GetMSIToken("https://management.azure.com/", tenantId);
                    await tokenCache.Set($"management.azure.com.{tenantId}", subscriptionId, token);
                }
                else
                {
                    token = cachedEntity.CachedValue;
                }
            }

            TokenCredentials creds = new TokenCredentials(token);
            return new AnalysisServicesManagementClient(creds) { SubscriptionId = subscriptionId };
        }

        /// <summary>
        /// Retrieves an access token for a client application to access the target service. NB: uses DEPRECATED api. Should be updated.
        /// </summary>
        /// <param name="tenantId">Azure AD tenant.</param>
        /// <param name="clientId">Azure AD client application identifier.</param>
        /// <param name="clientSecret">Azure AD client application secret.</param>
        /// <param name="serviceUri">Target service uri.</param>
        /// <returns>Access (Bearer) JWT.</returns>
        public static async Task<SecureString> GetServiceBearerToken(string tenantId, string clientId, SecureString clientSecret, string serviceUri)
        {
            var authContext = new AuthenticationContext($"https://login.microsoftonline.com/{tenantId}");
            var credential = new ClientCredential(clientId, SecureStringToString(clientSecret));
            AuthenticationResult result = await authContext.AcquireTokenAsync(serviceUri, credential);
            return StringToSecureString(result.AccessToken);
        }

        /// <summary>
        /// Refer to https://stackoverflow.com/questions/818704/how-to-convert-securestring-to-system-string. Converts secure string to normal string.
        /// </summary>
        /// <param name="secret">Secret to convert.</param>
        /// <returns>A regular string.</returns>
        public static string SecureStringToString(SecureString secret)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(secret);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        /// <summary>
        /// Converts normal string to SecureString.
        /// </summary>
        /// <param name="secret">string to convert.</param>
        /// <returns>An instance of <see cref="SecureString"/>.</returns>
        public static SecureString StringToSecureString(string secret)
        {
            SecureString result = new SecureString();

            foreach (char ch in secret)
            {
                result.AppendChar(ch);
            }

            return result;
        }
    }
}
