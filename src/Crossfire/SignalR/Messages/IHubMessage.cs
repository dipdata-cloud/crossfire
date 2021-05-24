// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Crossfire.SignalR.Messages
{
    /// <summary>
    /// SignalR hub response contract.
    /// </summary>
    public interface IHubMessage
    {
        /// <summary>
        /// Gets or sets data to send back to a client.
        /// </summary>
        string Payload { get; set; }

        /// <summary>
        /// Gets or sets <see cref="Background.Common.BackgroundJobParams.UserSubscriberName"/>.
        /// </summary>
        string UserSubscriberName { get; set; }
    }
}
