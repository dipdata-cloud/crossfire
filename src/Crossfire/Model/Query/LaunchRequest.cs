// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Crossfire.Extensions.CodeSecurity;
using Crossfire.Model.Base;

namespace Crossfire.Model.Query
{
    /// <summary>
    /// A request to launch an Azure AS instance.
    /// </summary>
    public sealed class LaunchRequest : JsonQueryRequest
    {
        /// <inheritdoc/>
        public override string GetSHA256()
        {
            return TextSecurity.ComputeHashString($"{this.UniqueClientIdentifier}.{this.RequestMetadata}");
        }
    }
}
