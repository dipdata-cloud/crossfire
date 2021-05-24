// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Crossfire.Config;
using Crossfire.Extensions;
using CrossfireXUnit.Local;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PyrosWeb.Extensions;
using Xunit;

namespace CrossfireXUnit.Clients
{
    /// <summary>
    /// Tests Graph API client.
    /// </summary>
    public class AzureAdB2CGraphClientTest : ParametizedTest
    {
        /// <summary>
        /// Tests <see cref="AzureAdB2CGraphClient.GetUserByObjectId(string)"/> for some existing user.
        /// </summary>
        /// <returns>XUnit test task.</returns>
        [Fact]
        public async Task GetUserByObjectIdExisting()
        {
            var objectId = this.TestConfiguration.AzureAdGraphApiTestUserId;
            var testClient = await this.GetClient();
            var stringUser = await testClient.GetUserByObjectId(objectId);
            var user = JsonConvert.DeserializeObject<JObject>(stringUser);
            Assert.Equal($"https://graph.windows.net/{this.TestConfiguration.Tenant}/$metadata#directoryObjects/@Element", user.Value<string>("odata.metadata"));
        }

        /// <summary>
        /// Tests <see cref="AzureAdB2CGraphClient.GetUserByObjectId(string)"/> for non-existing user.
        /// </summary>
        /// <returns>XUnit test task.</returns>
        [Fact]
        public async Task GetUserByObjectIdNonExisting()
        {
            var testClient = await this.GetClient();
            var stringUser = await testClient.GetUserByObjectId("non-existing");
            Assert.Null(stringUser);
        }

        /// <summary>
        /// Tests <see cref="UserPrincipalResolver.ResolveUserFromClaimsPrincipal(ClaimsPrincipal, AzureAdB2CGraphClient)"/>.
        /// </summary>
        /// <returns>XUnit test task.</returns>
        [Fact]
        public async Task ResolveUserFromClaimsPrincipal()
        {
            var (objectId, expectedUserId) = (this.TestConfiguration.AzureAdGraphApiTestUserId, this.TestConfiguration.AzureAdGraphApiTestUPN);
            var testClient = this.GetGraphClient();
            var claims = new List<Claim>()
            {
                new Claim(UserPrincipalResolver.IDCLAIM, objectId),
            };
            var claimsPrincipal = new ClaimsPrincipal(new List<ClaimsIdentity>() { new ClaimsIdentity(claims) });
            var userId = await UserPrincipalResolver.ResolveUserFromClaimsPrincipal(claimsPrincipal, testClient);

            Assert.Equal(expectedUserId, userId);
        }

        private AzureAdB2CGraphClient GetGraphClient()
        {
            var testConf = new AuthenticationConfig
            {
                AzureGraphApiTenant = this.TestConfiguration.Tenant,
                KeyVaultUri = this.TestConfiguration.KeyVault.Uri,
                AzureGraphClientId = this.TestConfiguration.AzureAdGraphApiClientId,
            };
            var logger = LoggerFactory.Create((c) => c.AddConsole()).CreateLogger<AzureAdB2CGraphClient>();
            return new AzureAdB2CGraphClient(testConf, logger, new HttpClient());
        }

        private async Task<AzureAdB2CGraphClient> GetClient()
        {
            var testClient = this.GetGraphClient();
            return await testClient.BuildClient();
        }
    }
}
