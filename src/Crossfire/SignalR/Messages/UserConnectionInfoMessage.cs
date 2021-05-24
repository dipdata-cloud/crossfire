// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Crossfire.SignalR.Messages
{
    /// <summary>
    /// A message bearing information on Azure AS servers a user has access to.
    /// </summary>
    public sealed class UserConnectionInfoMessage : IHubMessage
    {
        /// <summary>
        /// Gets or sets a list of <see cref="Config.ConnectedServerConfig"/> serialized as JSON object.
        /// </summary>
        public string Payload { get; set; }

        /// <inheritdoc/>
        public string UserSubscriberName { get; set; }
    }
}
