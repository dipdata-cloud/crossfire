// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PyrosWeb.Extensions;

namespace Crossfire.Extensions
{
    /// <summary>
    /// Resolves user object id in Azure AD (also B2C) to user principal id (they might not be the same).
    /// </summary>
    public static class UserPrincipalResolver
    {
        /// <summary>
        /// Name of a user identifier claim.
        /// </summary>
        public const string IDCLAIM = "http://schemas.microsoft.com/identity/claims/objectidentifier";

        /// <summary>
        /// Searches for user in the AD and returns user id, if any. Can return a values from a cache as well.
        /// </summary>
        /// <param name="user">Logged in user.</param>
        /// <param name="resolverClient">Graph API client.</param>
        /// <returns>User princial identifier in Azure AD or null.</returns>
        public static async Task<string> ResolveUserFromClaimsPrincipal(ClaimsPrincipal user, AzureAdB2CGraphClient resolverClient)
        {
            await resolverClient.BuildClient();
            string userObjectId = user.FindFirst(type: IDCLAIM).Value;

            if (!user.HasClaim(c => c.Type.Equals(IDCLAIM)))
            {
                return null;
            }

            string cachedUserId = GetUserFromCache(userObjectId, resolverClient);

            if (cachedUserId != null)
            {
                return cachedUserId;
            }

            string userId = await GetUserFromAD(userObjectId, resolverClient);

            if (userId != null)
            {
                resolverClient.UserPrincipalCache.Set(userObjectId, userId, TimeSpan.FromSeconds(3600));
                return userId;
            }

            return null;
        }

        private static string GetUserFromCache(string userName, AzureAdB2CGraphClient resolverClient)
        {
            if (!resolverClient.UserPrincipalCache.TryGetValue(userName, out string result))
            {
                return null;
            }

            return result;
        }

        private static async Task<string> GetUserFromAD(string userObjectId, AzureAdB2CGraphClient resolverClient)
        {
            var btcUserObj = await resolverClient.GetUserByObjectId(userObjectId);
            var btcUser = (JObject)JsonConvert.DeserializeObject(btcUserObj);
            return btcUser.Value<string>("userPrincipalName");
        }
    }
}
