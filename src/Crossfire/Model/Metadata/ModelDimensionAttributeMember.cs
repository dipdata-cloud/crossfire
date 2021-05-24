// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Crossfire.Model.Metadata.Base;
using Newtonsoft.Json;

namespace Crossfire.Model.Metadata
{
    /// <summary>
    /// An Azure AS Model attribute member.
    /// </summary>
    public sealed class ModelDimensionAttributeMember : ModelObject
    {
        private readonly string codeName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelDimensionAttributeMember"/> class.
        /// </summary>
        /// <param name="parent">Parent attribute.</param>
        /// <param name="codeName">Full MDX name of an attribute.</param>
        public ModelDimensionAttributeMember(ModelDimensionAttribute parent, string codeName)
        {
            this.Parent = parent;
            this.codeName = codeName;
        }

        /// <summary>
        /// Gets a parent attribute.
        /// </summary>
        [JsonIgnore]
        public ModelDimensionAttribute Parent { get; }

        /// <inheritdoc/>
        public override string CodeName => this.codeName;
    }
}
