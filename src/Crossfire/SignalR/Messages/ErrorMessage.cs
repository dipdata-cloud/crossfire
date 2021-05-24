// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Crossfire.Model.Base;

namespace Crossfire.SignalR.Messages
{
    /// <summary>
    /// Error message.
    /// </summary>
    public sealed class ErrorMessage : IHubMessage
    {
        /// <summary>
        /// Gets or sets information about what happened when processing a request.
        /// </summary>
        public string Payload { get; set; }

        /// <inheritdoc/>
        public string UserSubscriberName { get; set; }

        /// <summary>
        /// Gets or sets a request which caused this error.
        /// </summary>
        public JsonQueryRequest SubmittedRequest { get; set; }
    }
}
