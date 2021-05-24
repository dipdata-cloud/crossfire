// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Crossfire.Config;
using Crossfire.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json;

namespace PyrosWeb.Extensions
{
    /// <summary>
    /// Graph API client.
    /// </summary>
    public sealed class AzureAdB2CGraphClient
    {
        /// <summary>
        /// Base URI for Azure AD auth.
        /// </summary>
        public const string AADINSTANCE = "https://login.microsoftonline.com/";

        /// <summary>
        /// Base URI for Graph API auth.
        /// </summary>
        public const string AADGRAPHENDPOINT = "https://graph.windows.net";

        /// <summary>
        /// Graph API version to use.
        /// </summary>
        public const string AADGRAPHVERSION = "api-version=1.6";

        private readonly ILogger<AzureAdB2CGraphClient> logger;
        private readonly AuthenticationConfig authenticationConfig;
        private readonly HttpClient httpClient;
        private IConfidentialClientApplication b2cGraphClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureAdB2CGraphClient"/> class.
        /// </summary>
        /// <param name="authenticationConfig">Graph API config, <see cref="AuthenticationConfig"/>.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="httpClient">Http client.</param>
        public AzureAdB2CGraphClient(AuthenticationConfig authenticationConfig, ILogger<AzureAdB2CGraphClient> logger, HttpClient httpClient)
        {
            this.authenticationConfig = authenticationConfig;
            this.UserPrincipalCache = new MemoryCache(new MemoryCacheOptions() { CompactionPercentage = 0.2 });
            this.httpClient = httpClient;
            this.logger = logger;
        }

        /// <summary>
        /// Gets an in-memory cache for resolved user principals.
        /// </summary>
        public IMemoryCache UserPrincipalCache { get; internal set; }

        /// <summary>
        /// Async client builder.
        /// </summary>
        /// <returns>An instance of <see cref="AzureAdB2CGraphClient"/>.</returns>
        public async Task<AzureAdB2CGraphClient> BuildClient()
        {
            string clientSecret = IdentityManager.SecureStringToString(await IdentityManager.GetSecret(
                    keyVaultUri: this.authenticationConfig.KeyVaultUri,
                    secretId: this.authenticationConfig.AzureGraphClientId,
                    tenantId: this.authenticationConfig.AzureGraphApiTenant));

            this.b2cGraphClient = ConfidentialClientApplicationBuilder.Create(this.authenticationConfig.AzureGraphClientId)
                .WithClientSecret(clientSecret)
                .WithTenantId(this.authenticationConfig.AzureGraphApiTenant)
                .Build();

            return this;
        }

        /// <summary>
        /// Resolves Azure AD objectId into a user principal identifier.
        /// </summary>
        /// <param name="objectId">Azure AD object identifier.</param>
        /// <returns>UPN identifier.</returns>
        public async Task<string> GetUserByObjectId(string objectId)
        {
            return await this.SendGraphGetRequest("/users/" + objectId, null);
        }

        /// <summary>
        /// Sends an authenticated request to the Graph API.
        /// </summary>
        /// <param name="api">API path.</param>
        /// <param name="query">API query.</param>
        /// <returns>Response content as string or null if failed.</returns>
        public async Task<string> SendGraphGetRequest(string api, string query)
        {
            string url = $"{AADGRAPHENDPOINT}/{this.authenticationConfig.AzureGraphApiTenant}{api}?{AADGRAPHVERSION}";
            if (!string.IsNullOrEmpty(query))
            {
                url += "&" + query;
            }

            // Append the access token for the Graph API to the Authorization header of the request, using the Bearer scheme.
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await this.AcquireAccessToken());
            HttpResponseMessage response = await this.httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                object formatted = JsonConvert.DeserializeObject(error);
                this.logger.LogError(new WebException(JsonConvert.SerializeObject(formatted, Formatting.Indented)), "Error Calling the Graph API");
                return null;
            }

            return await response.Content.ReadAsStringAsync();
        }

        private async Task<string> AcquireAccessToken() => (await this.b2cGraphClient.AcquireTokenForClient(new List<string>
                {
                    "https://graph.windows.net/.default",
                }).ExecuteAsync()).AccessToken;
    }
}
