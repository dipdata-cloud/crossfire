// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Crossfire.Config;
using Xunit;

namespace CrossfireXUnit.Configs
{
    /// <summary>
    /// Tests for configuration class for Azure AS.
    /// </summary>
    public class ConnectedServerConfigTest
    {
        /// <summary>
        /// Tests correctness of a server FQN string.
        /// </summary>
        /// <param name="region">Azure AS region.</param>
        /// <param name="resourceGroup">Azure resource group.</param>
        /// <param name="serverName">Azure AS server name.</param>
        [Theory]
        [InlineData("test-r", "test-rg", "test-srv")]
        public void ServerQualifiedName(string region, string resourceGroup, string serverName)
        {
            var dummyConf = new ConnectedServerConfig
            {
                Region = region,
                ResourceGroup = resourceGroup,
                ServerName = serverName,
            };

            Assert.Equal($"{region}.{resourceGroup}.{serverName}".ToLowerInvariant(), dummyConf.ServerQualifiedName);
        }
    }
}
