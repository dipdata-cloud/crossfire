// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Crossfire.SignalR.Hubs;
using CrossfireXUnit.Local;
using Moq;
using Xunit;

namespace CrossfireXUnit.SignalR
{
    /// <summary>
    /// SignalR hub tests.
    /// </summary>
    public class ModelMessageHubTest : SignalRTest
    {
        /// <summary>
        /// A hub name.
        /// </summary>
        [Fact]
        public void HubName()
        {
            Assert.Equal("modelMessages", ModelMessageHub.HubName);
        }

        /// <summary>
        /// Tests <see cref="ModelMessageHub.GetChannel(string, HubChannels)"/>.
        /// </summary>
        /// <param name="channelId">Channel name.</param>
        /// <param name="channelType">Channel type.</param>
        [Theory]
        [InlineData("test", HubChannels.JOB_CHANNEL)]
        public void GetChannel(string channelId, HubChannels channelType)
        {
            var expected = $"{channelType}.{channelId}";
            Assert.Equal(expected, ModelMessageHub.GetChannel(channelId, channelType));
        }

        /// <summary>
        /// Tests <see cref="ModelMessageHub.JoinServiceChannelJsClient(string)"/>.
        /// </summary>
        /// <param name="userPrincipalName">User principal name of a client.</param>
        /// <returns>XUnit test task.</returns>
        [Theory]
        [InlineData("test")]
        [InlineData("")]
        public async Task JoinServiceChannel(string userPrincipalName)
        {
            var (mockClientProxy, _, hub) = this.SetupMockHub();

            await hub.JoinServiceChannelJsClient(userPrincipalName);

            mockClientProxy.Verify(
                clientProxy => clientProxy.SendCoreAsync(
                    ModelMessageHub.CLIENTID_RESPONSE_MESSAGE,
                    It.Is<object[]>(o => o != null && (string)o[0] == userPrincipalName),
                    default(CancellationToken)),
                Times.Once);
        }

        /// <summary>
        /// Tests <see cref="ModelMessageHub.JoinModelQueryChannelJsClient(string, string)(string)"/>.
        /// </summary>
        /// <param name="userPrincipalName">User principal name of a client.</param>
        /// <param name="clientUniqueIdentifier">Connection id (dummy).</param>
        /// <returns>XUnit test task.</returns>
        [Theory]
        [InlineData("test", "2ed7ca31-fc8a-479c-b07b-4b606c09eebe")]
        [InlineData("", "2ed7ca31-fc8a-479c-b07b-4b606c09eebe")]
        public async Task JoinModelQueryChannel(string userPrincipalName, string clientUniqueIdentifier)
        {
            var (_, mockGroups, hub) = this.SetupMockHub();

            await hub.JoinModelQueryChannelJsClient(userPrincipalName, clientUniqueIdentifier);
            var expectedGroup = ModelMessageHub.GetGroupIdentifier(userPrincipalName, clientUniqueIdentifier);

            mockGroups.Verify(
                groups => groups.AddToGroupAsync(It.Is<string>(v => v == Guid.Empty.ToString()), It.Is<string>(v => v == ModelMessageHub.GetChannel(expectedGroup, HubChannels.QUERY_CHANNEL)), default(CancellationToken)),
                Times.Once);
        }
    }
}
