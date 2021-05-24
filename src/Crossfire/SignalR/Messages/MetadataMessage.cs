// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Crossfire.SignalR.Messages
{
    /// <summary>
    /// Message containing Azure AS model metadata.
    /// </summary>
    public sealed class MetadataMessage : IHubMessage
    {
        /// <summary>
        /// Gets or sets model metadata, serialized as JSON object.
        /// </summary>
        public string Payload { get; set; }

        /// <inheritdoc/>
        public string UserSubscriberName { get; set; }
    }
}
