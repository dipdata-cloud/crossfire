// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Crossfire.Extensions.CodeSecurity;
using Crossfire.Model.Base;

namespace Crossfire.Model.Metadata
{
    /// <summary>
    /// A request to retrieve Azure AS model metadata.
    /// </summary>
    public sealed class ModelMetadataRequest : JsonQueryRequest
    {
        /// <inheritdoc/>
        public override string GetSHA256()
        {
            return TextSecurity.ComputeHashString($"{this.ResourceGroup}.{this.Region}.{this.TargetServer}.{this.TargetDatabase}.{this.RequestMetadata}");
        }
    }
}
