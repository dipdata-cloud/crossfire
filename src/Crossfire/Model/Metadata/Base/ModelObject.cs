// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Crossfire.Model.Metadata.Base
{
    /// <summary>
    /// Base class for Azure AS model elements.
    /// </summary>
    public abstract class ModelObject
    {
        /// <summary>
        /// Gets or sets an Azure AS Model element name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets an Azure AS Model element DAX/MDX member name.
        /// </summary>
        public virtual string CodeName => $"[{this.Name}]";
    }
}
