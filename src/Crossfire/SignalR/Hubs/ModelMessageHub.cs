// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Crossfire.Extensions.CodeSecurity;
using Microsoft.AspNetCore.SignalR;

namespace Crossfire.SignalR.Hubs
{
    /// <summary>
    /// All hub channels.
    /// </summary>
    public enum HubChannels
    {
        /// <summary>
        /// Channel where server health messages are communicated.
        /// </summary>
        [EnumMember(Value = "hearbeat")]
        HEARTBEAT_CHANNEL,

        /// <summary>
        /// Job control channel.
        /// </summary>
        [EnumMember(Value = "jobs")]
        JOB_CHANNEL,

        /// <summary>
        /// Query response communication channel.
        /// </summary>
        [EnumMember(Value = "query")]
        QUERY_CHANNEL,

        /// <summary>
        /// Model metadata response communication channel.
        /// </summary>
        [EnumMember(Value = "metadata")]
        METADATA_CHANNEL,

        /// <summary>
        /// User connection info channel.
        /// </summary>
        [EnumMember(Value = "info")]
        INFO_CHANNEL,

        /// <summary>
        /// Gateway channel.
        /// </summary>
        [EnumMember(Value = "service")]
        SERVICE_CHANNEL,

        /// <summary>
        /// Error channel.
        /// </summary>
        [EnumMember(Value = "error")]
        ERROR_CHANNEL,
    }

    /// <summary>
    /// SignalR hub that communicates job results to connected clients.
    /// </summary>
    public sealed class ModelMessageHub : Hub
    {
        /// <summary>
        /// Alias for a query response method.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string QUERY_RESPONSE_MESSAGE = "queryResult";

        /// <summary>
        /// Alias for a heartbeat broadcast method.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string HEARTBEAT_BROADCAST_MESSAGE = "heartBeat";

        /// <summary>
        /// Alias for a job status response method.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string JOBSTATUS_RESPONSE_MESSAGE = "jobStatus";

        /// <summary>
        /// Alias for a client id generator method.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string CLIENTID_RESPONSE_MESSAGE = "clientIdentifier";

        /// <summary>
        /// Alias for a metadata response method.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string METADATA_RESPONSE_MESSAGE = "modelMetadata";

        /// <summary>
        /// Alias for a user connection info response method.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string CONNECTIONINFO_RESPONSE_MESSAGE = "connectionInfo";

        /// <summary>
        /// Alias for a service response method.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string SERVICE_RESPONSE_MESSAGE = "serviceMessage";

        /// <summary>
        /// Alias for an error response method.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string ERROR_RESPONSE_MESSAGE = "errorMessage";

        /// <summary>
        /// Alias for a method to join a query channel.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string JOIN_MODELQUERYCHANNEL = "JoinModelQueryChannel";

        /// <summary>
        /// Alias for a method to join a heartbeat channel.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string JOIN_HEARTBEATCHANNEL = "JoinHeartbeatNotificationsChannel";

        /// <summary>
        /// Alias for a method to join a metadata channel.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string JOIN_METADATACHANNEL = "JoinModelMetadataChannel";

        /// <summary>
        /// Alias for a method to join a service channel.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string JOIN_SERVICECHANNEL = "JoinServiceChannel";

        /// <summary>
        /// Alias for a method to join an error channel.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string JOIN_ERRORCHANNEL = "JoinErrorChannel";

        /// <summary>
        /// Gets hub name.
        /// </summary>
        public static string HubName => "modelMessages";

        /// <summary>
        /// Gets a channel name.
        /// </summary>
        /// <param name="channelId">Base channel prefix.</param>
        /// <param name="channelType">Channel type.</param>
        /// <returns>Channel name in this hub.</returns>
        public static string GetChannel(string channelId, HubChannels channelType) => $"{channelType}.{channelId}";

        /// <summary>
        /// Gets a SignalR group identifier.
        /// </summary>
        /// <param name="userPrincipalName"><see cref="Background.Common.BackgroundJobParams.UserPrincipalName"/>.</param>
        /// <param name="clientUniqueIdentifier">A unique connection identifier issued by this hub.</param>
        /// <returns>Group name in this hub.</returns>
        public static string GetGroupIdentifier(string userPrincipalName, string clientUniqueIdentifier) => TextSecurity.ComputeHashString($"{userPrincipalName}_{clientUniqueIdentifier}");

        /// <summary>
        /// Client method for joining a query channel.
        /// </summary>
        /// <param name="clientUniqueIdentifier">A unique connection identifier issued by this hub.</param>
        /// <returns>A <see cref="Task"/> representing the AddToGroupAsync operation.</returns>
        [ExcludeFromCodeCoverage]
        public async Task JoinModelQueryChannel(string clientUniqueIdentifier)
        {
            string userPrincipalName = this.Context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            string groupName = GetGroupIdentifier(userPrincipalName, clientUniqueIdentifier);
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, GetChannel(groupName, HubChannels.QUERY_CHANNEL));
        }

        /// <summary>
        /// Client method for joining a query channel when UPN cannot be provided from HubCallerContext.
        /// </summary>
        /// <param name="userPrincipalName"><see cref="Background.Common.BackgroundJobParams.UserPrincipalName"/>.</param>
        /// <param name="clientUniqueIdentifier">A unique connection identifier issued by this hub.</param>
        /// <returns>A <see cref="Task"/> representing the AddToGroupAsync operation.</returns>
        public async Task JoinModelQueryChannelJsClient(string userPrincipalName, string clientUniqueIdentifier)
        {
            string groupName = GetGroupIdentifier(userPrincipalName, clientUniqueIdentifier);
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, GetChannel(groupName, HubChannels.QUERY_CHANNEL));
        }

        /// <summary>
        /// A client method for joining an error channel.
        /// </summary>
        /// <param name="clientUniqueIdentifier">A unique connection identifier issued by this hub.</param>
        /// <returns>A <see cref="Task"/> representing the AddToGroupAsync operation.</returns>
        [ExcludeFromCodeCoverage]
        public async Task JoinErrorChannel(string clientUniqueIdentifier)
        {
            string userPrincipalName = this.Context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            string groupName = GetGroupIdentifier(userPrincipalName, clientUniqueIdentifier);
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, GetChannel(groupName, HubChannels.ERROR_CHANNEL));
        }

        /// <summary>
        /// A client method for joining an error channel when UPN cannot be provided from HubCallerContext.
        /// </summary>
        /// <param name="userPrincipalName"><see cref="Background.Common.BackgroundJobParams.UserPrincipalName"/>.</param>
        /// <param name="clientUniqueIdentifier">A unique connection identifier issued by this hub.</param>
        /// <returns>A <see cref="Task"/> representing the AddToGroupAsync operation.</returns>
        public async Task JoinErrorChannelJsClient(string userPrincipalName, string clientUniqueIdentifier)
        {
            string groupName = GetGroupIdentifier(userPrincipalName, clientUniqueIdentifier);
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, GetChannel(groupName, HubChannels.ERROR_CHANNEL));
        }

        /// <summary>
        /// A client method for joining a user connection info channel.
        /// </summary>
        /// <param name="userPrincipalName"><see cref="Background.Common.BackgroundJobParams.UserPrincipalName"/>.</param>
        /// <param name="clientUniqueIdentifier">A unique connection identifier issued by this hub.</param>
        /// <returns>A <see cref="Task"/> representing the AddToGroupAsync operation.</returns>
        public async Task JoinConnectionInfoChannelJsClient(string userPrincipalName, string clientUniqueIdentifier)
        {
            string groupName = GetGroupIdentifier(userPrincipalName, clientUniqueIdentifier);
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, GetChannel(groupName, HubChannels.INFO_CHANNEL));
        }

        /// <summary>
        /// A client method to join a gateway channel.
        /// </summary>
        /// <returns>A task that sends <see cref="CLIENTID_RESPONSE_MESSAGE"/> back to the client.</returns>
        [ExcludeFromCodeCoverage]
        public async Task JoinServiceChannel()
        {
            string userPrincipalName = this.Context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            string clientUniqueIdentifier = Guid.NewGuid().ToString();
            string groupName = GetGroupIdentifier(userPrincipalName, clientUniqueIdentifier);

            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, GetChannel(groupName, HubChannels.SERVICE_CHANNEL));

            await this.SendClientIdentifier(userPrincipalName, clientUniqueIdentifier);
        }

        /// <summary>
        /// A client method to join a gateway channel when UPN cannot be provided from HubCallerContext.
        /// </summary>
        /// <param name="userPrincipalName"><see cref="Background.Common.BackgroundJobParams.UserPrincipalName"/>.</param>
        /// <returns>A task that sends <see cref="CLIENTID_RESPONSE_MESSAGE"/> back to the client.</returns>
        public async Task JoinServiceChannelJsClient(string userPrincipalName)
        {
            string clientUniqueIdentifier = Guid.NewGuid().ToString();
            string groupName = GetGroupIdentifier(userPrincipalName, clientUniqueIdentifier);

            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, GetChannel(groupName, HubChannels.SERVICE_CHANNEL));

            await this.SendClientIdentifier(userPrincipalName, clientUniqueIdentifier);
        }

        /// <summary>
        /// A client method to join a job control channel.
        /// </summary>
        /// <param name="clientUniqueIdentifier">A unique connection identifier issued by this hub.</param>
        /// <returns>A <see cref="Task"/> representing the AddToGroupAsync operation.</returns>
        [ExcludeFromCodeCoverage]
        public async Task JoinJobNotificationsChannel(string clientUniqueIdentifier)
        {
            string userPrincipalName = this.Context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            string groupName = GetGroupIdentifier(userPrincipalName, clientUniqueIdentifier);
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, GetChannel(groupName, HubChannels.JOB_CHANNEL));
        }

        /// <summary>
        /// A client method to join a heartbeat notifications channel.
        /// </summary>
        /// <param name="userPrincipalName"><see cref="Background.Common.BackgroundJobParams.UserPrincipalName"/>.</param>
        /// <param name="clientUniqueIdentifier">A unique connection identifier issued by this hub.</param>
        /// <returns>A <see cref="Task"/> representing the AddToGroupAsync operation.</returns>
        [ExcludeFromCodeCoverage]
        public async Task JoinHeartbeatNotificationsChannel(string userPrincipalName, string clientUniqueIdentifier)
        {
            string groupName = GetGroupIdentifier(userPrincipalName, clientUniqueIdentifier);
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, GetChannel(groupName, HubChannels.HEARTBEAT_CHANNEL));
        }

        /// <summary>
        /// A client method to join a metadata channel.
        /// </summary>
        /// <param name="clientUniqueIdentifier">A unique connection identifier issued by this hub.</param>
        /// <returns>A <see cref="Task"/> representing the AddToGroupAsync operation.</returns>
        [ExcludeFromCodeCoverage]
        public async Task JoinModelMetadataChannel(string clientUniqueIdentifier)
        {
            string userPrincipalName = this.Context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            string groupName = GetGroupIdentifier(userPrincipalName, clientUniqueIdentifier);
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, GetChannel(groupName, HubChannels.METADATA_CHANNEL));
        }

        /// <summary>
        /// A client method to join a metadata channel when UPN cannot be provided from HubCallerContext.
        /// </summary>
        /// <param name="userPrincipalName"><see cref="Background.Common.BackgroundJobParams.UserPrincipalName"/>.</param>
        /// <param name="clientUniqueIdentifier">A unique connection identifier issued by this hub.</param>
        /// <returns>A <see cref="Task"/> representing the AddToGroupAsync operation.</returns>
        public async Task JoinModelMetadataChannelJsClient(string userPrincipalName, string clientUniqueIdentifier)
        {
            string groupName = GetGroupIdentifier(userPrincipalName, clientUniqueIdentifier);
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, GetChannel(groupName, HubChannels.METADATA_CHANNEL));
        }

        // send unique id for the connected client to use
        private async Task SendClientIdentifier(string userPrincipalName, string clientUniqueIdentifier)
        {
            await this.Clients.Caller.SendAsync(CLIENTID_RESPONSE_MESSAGE, userPrincipalName, clientUniqueIdentifier);
        }
    }
}
