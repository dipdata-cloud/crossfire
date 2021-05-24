// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Crossfire.SignalR.Messages
{
    /// <summary>
    /// A message bearing a query result.
    /// </summary>
    public sealed class QueryResultMessage : IHubMessage
    {
        /// <summary>
        /// Gets or sets a query result, serialized as JSON object.
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// Gets or sets metadata passed along with request by a client.
        /// </summary>
        public string QueryMetadata { get; set; }

        /// <inheritdoc/>
        public string UserSubscriberName { get; set; }
    }
}
