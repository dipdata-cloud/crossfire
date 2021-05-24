// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Crossfire.Extensions.CodeSecurity;
using Crossfire.Model.Base;

namespace Crossfire.Model
{
    /// <summary>
    /// A Request to retrieve Azure AS models a user has access to.
    /// </summary>
    public class UserConnectionInfoRequest : JsonQueryRequest
    {
        /// <summary>
        /// Gets or sets search hints.
        /// </summary>
        public string[] SearchServers { get; set; }

        /// <inheritdoc/>
        public override string GetSHA256()
        {
            return TextSecurity.ComputeHashString($"{this.RequestMetadata}");
        }
    }
}
