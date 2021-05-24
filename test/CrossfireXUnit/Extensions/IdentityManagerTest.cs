// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Crossfire.Extensions;
using CrossfireXUnit.Local;
using Microsoft.Azure.Management.Analysis;
using xRetry;
using Xunit;

namespace CrossfireXUnit.Extensions
{
    /// <summary>
    /// Tests Azure AD interactions and helper methods.
    /// </summary>
    public class IdentityManagerTest : StorageTest
    {
        /// <summary>
        /// Tests a conversion between string and secure string.
        /// </summary>
        /// <param name="input">Input string.</param>
        [Theory]
        [InlineData("abc")]
        public void SecureStringToString(string input)
        {
            var secure = IdentityManager.StringToSecureString(input);
            Assert.Equal(input, IdentityManager.SecureStringToString(secure));
        }

        /// <summary>
        /// Tests GetSecret method.
        /// </summary>
        [Fact]
        public void GetSecret()
        {
            var secrets = this.TestConfiguration.KeyVault.TestSecrets
                .Select(secret => IdentityManager.GetSecret(this.TestConfiguration.KeyVault.Uri, secret.Name, this.TestConfiguration.Tenant).Result)
                .Select(ss => IdentityManager.SecureStringToString(ss))
                .ToList();

            var expectedSecrets = this.TestConfiguration.KeyVault.TestSecrets
                .Select(s => s.Secret)
                .ToList();

            Assert.Equal(expectedSecrets, secrets);
        }

        /// <summary>
        /// Tests MSI token fetch with GetMSIToken.
        /// </summary>
        /// <returns>XUnit test task.</returns>
        [Fact]
        public async Task GetMSIToken()
        {
            var token = await IdentityManager.GetMSIToken("https://management.azure.com", this.TestConfiguration.Tenant);
            Assert.NotEqual(string.Empty, token);
        }

        /// <summary>
        /// Tests AS Management client instantiator.
        /// </summary>
        /// <param name="cacheTableName">Table name for cache client.</param>
        /// <returns>XUnit test task.</returns>
        [RetryTheory(3)]
        [InlineData("")]
        [InlineData(nameof(IdentityManagerTest))]
        public async Task CreateAnalysisServicesManagementClient(string cacheTableName)
        {
            AnalysisServicesManagementClient client;
            client = await IdentityManager.CreateAnalysisServicesManagementClient("test-sub", this.TestConfiguration.Tenant);
            if (cacheTableName != string.Empty)
            {
                await this.TestStorageContext.CreateTable(cacheTableName);
                client = await IdentityManager.CreateAnalysisServicesManagementClient("test-sub", this.TestConfiguration.Tenant, new CachedEntityClient<string>(this.TestStorageContext, cacheTableName));
            }

            Assert.NotNull(client);
        }
    }
}
