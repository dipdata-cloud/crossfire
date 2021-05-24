// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Crossfire.Model.Metadata.Base;
using Newtonsoft.Json;

namespace Crossfire.Model.Metadata
{
    /// <summary>
    /// Azure AS Model dimension attribute.
    /// </summary>
    public sealed class ModelDimensionAttribute : ModelObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelDimensionAttribute"/> class.
        /// </summary>
        /// <param name="parent">Parent dimension.</param>
        public ModelDimensionAttribute(ModelDimension parent)
        {
            this.AttributeMembers = new List<ModelDimensionAttributeMember>();
            this.Parent = parent;
        }

        /// <summary>
        /// Gets a dimension that owns this attribute.
        /// </summary>
        [JsonIgnore]
        public ModelDimension Parent { get; }

        /// <summary>
        /// Gets an MDX member name.
        /// </summary>
        public override string CodeName => $"{this.Parent.CodeName}.[{this.Name}]";

        /// <summary>
        /// Gets an MDX expression for all values of this attribute.
        /// </summary>
        public string AllMembersCodeName => $"{this.CodeName}.[All].children";

        /// <summary>
        /// Gets an MDX expression for showing an inner representation of an attribute.
        /// </summary>
        public string UniqueMemberCodeName => $"{this.CodeName}.currentmember.uniquename";

        /// <summary>
        /// Gets a list of attribute values.
        /// </summary>
        public List<ModelDimensionAttributeMember> AttributeMembers { get; private set; }
    }
}
