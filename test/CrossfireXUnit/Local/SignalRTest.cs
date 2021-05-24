// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Crossfire.Config;
using Crossfire.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace CrossfireXUnit.Local
{
    /// <summary>
    /// Base class for tests that require a SignalR hub.
    /// </summary>
    public abstract class SignalRTest : ParametizedTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SignalRTest"/> class.
        /// </summary>
        public SignalRTest()
            : base()
        {
            this.TestServers = new ConnectedServerConfig[]
            {
                new ConnectedServerConfig
                {
                      Region = this.TestConfiguration.AzureAS.Region,
                      ApplicationId = this.TestConfiguration.AzureAS.ApplicationId,
                      ResourceGroup = this.TestConfiguration.AzureAS.ResourceGroup,
                      ServerName = this.TestConfiguration.AzureAS.TargetServer,
                      ServerUri = this.TestConfiguration.AzureAS.ServerUri,
                      ServerServiceUri = this.TestConfiguration.AzureAS.ServerServiceUri,
                      SubscriptionId = this.TestConfiguration.AzureAS.SubscriptionId,
                      TenantId = this.TestConfiguration.Tenant,
                },
            };
        }

        /// <summary>
        /// Gets dummy Azure AS server config.
        /// </summary>
        public ConnectedServerConfig[] TestServers { get; }

        /// <summary>
        /// Generates SignalR hub and a connected client behaviour mock.
        /// </summary>
        /// <returns>Client proxy, group manager and a hub.</returns>
        public Tuple<Mock<IClientProxy>, Mock<IGroupManager>, ModelMessageHub> SetupMockHub()
        {
            var mockClients = new Mock<IHubCallerClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            var mockGroups = new Mock<IGroupManager>();
            var mockContext = new Mock<HubCallerContext>();
            mockClientProxy.CallBase = true;

            mockClients.Setup(clients => clients.All).Returns(mockClientProxy.Object);
            mockClients.Setup(clients => clients.Caller).Returns(mockClientProxy.Object);
            mockContext.Setup(c => c.ConnectionId).Returns(Guid.Empty.ToString());

            var hub = new ModelMessageHub()
            {
                Clients = mockClients.Object,
                Groups = mockGroups.Object,
                Context = mockContext.Object,
            };

            return new Tuple<Mock<IClientProxy>, Mock<IGroupManager>, ModelMessageHub>(mockClientProxy, mockGroups, hub);
        }

        /// <summary>
        /// Generates mocked IHubContext that utilizes a mocked client proxy.
        /// </summary>
        /// <returns>A hub context and a client proxy connected to that context.</returns>
        public Tuple<Mock<IHubContext<ModelMessageHub>>, Mock<IClientProxy>> SetupHubContext()
        {
            var mockContext = new Mock<IHubContext<ModelMessageHub>>();
            var (client, groupManager, hub) = this.SetupMockHub();

            mockContext.Setup(c => c.Clients.Client(Guid.Empty.ToString())).Returns(client.Object);
            mockContext.Setup(c => c.Clients.Group(It.IsAny<string>())).Returns(client.Object);
            return new Tuple<Mock<IHubContext<ModelMessageHub>>, Mock<IClientProxy>>(mockContext, client);
        }
    }
}
