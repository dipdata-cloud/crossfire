// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Crossfire.Model.Metadata.Base;

namespace Crossfire.Model.Metadata
{
    /// <summary>
    /// Azure AS Model measure.
    /// </summary>
    public sealed class ModelMeasure : ModelObject
    {
        /// <inheritdoc/>
        public override string CodeName => $"[Measures].[{this.Name}]";
    }
}
