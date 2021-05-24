// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Crossfire.SignalR.Messages
{
    /// <summary>
    /// Azure AS server health state message.
    /// </summary>
    public sealed class HeartbeatMessage : IHubMessage
    {
        /// <inheritdoc/>
        public string Payload { get; set; }

        /// <inheritdoc/>
        public string UserSubscriberName { get; set; }

        /// <summary>
        /// Gets or sets timestamp of this health check.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets server FQN that was a target of this check.
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Gets or sets the result of health check.
        /// </summary>
        public string HeartbeatState { get; set; }

        /// <summary>
        /// Gets or sets a unique server hash to be used by a client.
        /// </summary>
        public string ServerHash { get; set; }
    }
}
