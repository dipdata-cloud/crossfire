// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Crossfire.Model.Metadata.Base;

namespace Crossfire.Model.Metadata
{
    /// <summary>
    /// Represents an Azure AS Model Dimension (table).
    /// </summary>
    public sealed class ModelDimension : ModelObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelDimension"/> class.
        /// </summary>
        public ModelDimension()
        {
            this.Attributes = new List<ModelDimensionAttribute>();
        }

        /// <summary>
        /// Gets dimensions attributes (table fields).
        /// </summary>
        public List<ModelDimensionAttribute> Attributes { get; private set; }
    }
}
