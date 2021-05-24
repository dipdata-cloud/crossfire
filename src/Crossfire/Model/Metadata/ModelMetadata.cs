// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AnalysisServices.Tabular;

namespace Crossfire.Model.Metadata
{
    /// <summary>
    /// Azure AS Model structure.
    /// </summary>
    public sealed class ModelMetadata
    {
        /// <summary>
        /// Gets all model dimensions.
        /// </summary>
        public ModelDimension[] Dimensions { get; private set; }

        /// <summary>
        /// Gets all model measures.
        /// </summary>
        public ModelMeasure[] Measures { get; private set; }

        /// <summary>
        /// Gets or sets a model name.
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of a last update.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Fills the <see cref="Dimensions"/> array.
        /// </summary>
        /// <param name="modelTables">All model tables.</param>
        public void ImportDimensions(TableCollection modelTables)
        {
            Stack<ModelDimension> dimensions = new Stack<ModelDimension>();

            foreach (Table tbl in modelTables)
            {
                // skip hidden tables
                if (!tbl.IsHidden)
                {
                    ModelDimension dim = new ModelDimension { Name = tbl.Name };

                    foreach (var col in tbl.Columns)
                    {
                        if (!col.IsHidden)
                        {
                            ModelDimensionAttribute attr = new ModelDimensionAttribute(dim) { Name = col.Name };
                            dim.Attributes.Add(attr);
                        }
                    }

                    // completed dimension to the list
                    dimensions.Push(dim);
                }
            }

            this.Dimensions = dimensions.ToArray();
        }

        /// <summary>
        /// Fills the <see cref="Measures"/> array.
        /// </summary>
        /// <param name="modelTables">All model tables.</param>
        public void ImportMeasures(TableCollection modelTables)
        {
            Stack<ModelMeasure> measures = new Stack<ModelMeasure>();

            foreach (Table tbl in modelTables)
            {
                foreach (var measure in tbl.Measures)
                {
                    measures.Push(new ModelMeasure { Name = measure.Name });
                }
            }

            this.Measures = measures.ToArray();
        }
    }
}
