// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using Crossfire.Background.Common;
using Crossfire.Extensions;
using Crossfire.Model.Metadata;

namespace Crossfire.Background.JobSettings
{
    /// <summary>
    /// Configuration for <see cref="UserConnectionInfoJob"/>.
    /// </summary>
    public sealed class UserConnectionInfoJobParams : BackgroundJobParams
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserConnectionInfoJobParams"/> class.
        /// </summary>
        public UserConnectionInfoJobParams()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserConnectionInfoJobParams"/> class.
        /// </summary>
        /// <param name="root">Configuration to initialize from.</param>
        public UserConnectionInfoJobParams(BackgroundJobParams root)
            : base()
        {
            this.UserPrincipalName = root.UserPrincipalName;
            this.UserSubscriberName = root.UserSubscriberName;
            this.ConnectedServers = root.ConnectedServers;
            this.KeyVaultUri = root.KeyVaultUri;
        }

        /// <summary>
        /// Retrieves connection information from a distributed cache.
        /// </summary>
        /// <param name="infoCache">Distributed cache client (injected).</param>
        /// <returns>An array of <see cref="UserConnectionInfo"/>, if any information is found. Empty array otherwise.</returns>
        public async Task<UserConnectionInfo[]> GetCachedInfo(CachedEntityClient<UserConnectionInfo[]> infoCache)
        {
            var cachedInfo = await infoCache.Get(this.UserPrincipalName);
            if (cachedInfo == null)
            {
                return new UserConnectionInfo[] { };
            }
            else
            {
                return cachedInfo.CachedValue;
            }
        }
    }
}
