// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using Crossfire.Extensions.CodeSecurity;
using Crossfire.Model.Base;

namespace Crossfire.Model.Query
{
    /// <summary>
    /// Supported query languages.
    /// </summary>
    public enum CompilationTargets
    {
        /// <summary>
        /// MDX language.
        /// </summary>
        MDX = 0,

        /// <summary>
        /// DAX language.
        /// </summary>
        DAX = 1,
    }

    /// <summary>
    /// A request to execute a query against Azure AS Model.
    /// </summary>
    public sealed class QueryRequest : JsonQueryRequest
    {
        /// <summary>
        /// Gets or sets filters to apply in a query.
        /// </summary>
        public string[] QueryFilters { get; set; }

        /// <summary>
        /// Gets or sets dimensions to compute measures against.
        /// </summary>
        public string[] QuerySlices { get; set; }

        /// <summary>
        /// Gets or sets measures to compute.
        /// </summary>
        public string[] QueryValues { get; set; }

        /// <summary>
        /// Gets or sets custom dimensions or sets to define in a query.
        /// </summary>
        public string[] CustomSets { get; set; }

        /// <summary>
        /// Gets or sets custom measures to define in a query.
        /// </summary>
        public string[] CustomMembers { get; set; }

        /// <summary>
        /// Gets or sets a measure used to filter out empty values.
        /// </summary>
        public string DefaultMeasure { get; set; }

        /// <summary>
        /// Gets or sets a target model name.
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Gets or sets language of a query.
        /// </summary>
        public CompilationTargets CompilationTarget { get; set; }

        /// <summary>
        /// Gets or sets response format of a result received from SignalR hub.
        /// </summary>
        public int OutputFormat { get; set; }

        /// <summary>
        /// Convert request fields to a corresponding language query.
        /// </summary>
        /// <returns>Text query.</returns>
        public string Compile()
        {
            return this.CompilationTarget switch
            {
                CompilationTargets.MDX => this.MDXCompile(),
                CompilationTargets.DAX => this.DAXCompile(),
                _ => this.MDXCompile(),
            };
        }

        /// <inheritdoc/>
        public override string GetSHA256()
        {
            var filters = string.Join("#", this.QueryFilters ?? new string[] { });
            var slices = string.Join("#", this.QuerySlices ?? new string[] { });
            var values = string.Join("#", this.QueryValues);
            var customMembers = string.Join("#", this.CustomMembers ?? new string[] { });
            var customSets = string.Join("#", this.CustomSets ?? new string[] { });
            return TextSecurity.ComputeHashString(
                $"{filters}.{slices}.{values}.{customMembers}.{customSets}.{this.ModelName}.{this.CompilationTarget}.{this.OutputFormat}.{this.DefaultMeasure ?? string.Empty}");
        }

        private string MDXCompile()
        {
            string filterSection = string.Empty;
            int axisNumber = 0;
            if (this.QueryFilters?.Any() == true)
            {
                foreach (string queryFilter in this.QueryFilters)
                {
                    filterSection += $"{queryFilter} on {axisNumber}";
                    if (axisNumber < this.QueryFilters.Length - 1)
                    {
                        filterSection += ", ";
                    }

                    axisNumber += 1;
                }
            }

            int sliceNumber = 0;
            string slicerSection = string.Empty;

            if (this.QuerySlices?.Any() == true)
            {
                if (this.DefaultMeasure != null)
                {
                    slicerSection = "nonempty( (";
                }
                else
                {
                    slicerSection = "(";
                }

                foreach (string slicer in this.QuerySlices)
                {
                    slicerSection += $"{slicer}";
                    if (sliceNumber < this.QuerySlices.Length - 1)
                    {
                        slicerSection += ", ";
                    }

                    sliceNumber += 1;
                }

                if (this.DefaultMeasure != null)
                {
                    slicerSection += $"), {this.DefaultMeasure})";
                }
                else
                {
                    slicerSection += ")";
                }
            }

            string valuesSection = string.Empty;
            int valueNumber = 0;
            foreach (string value in this.QueryValues)
            {
                valuesSection += value;
                if (valueNumber < this.QueryValues.Length - 1)
                {
                    valuesSection += ", ";
                }

                valueNumber += 1;
            }

            string customSection = string.Empty;
            if ((this.CustomSets?.Any() == true) || (this.CustomMembers?.Any() == true))
            {
                customSection = $"with {Environment.NewLine}";

                if (this.CustomSets?.Any() == true)
                {
                    foreach (string customSet in this.CustomSets)
                    {
                        customSection += $"set {customSet}{Environment.NewLine}";
                    }
                }

                if (this.CustomMembers?.Any() == true)
                {
                    foreach (string customMember in this.CustomMembers)
                    {
                        customSection += $"member {customMember}{Environment.NewLine}";
                    }
                }
            }

            slicerSection = slicerSection != string.Empty ? $", {slicerSection} on 1" : string.Empty;
            return $"{customSection} select non empty {{ {valuesSection} }} on 0{slicerSection} from ( select {filterSection} from [{this.ModelName}] )";
        }

        private string DAXCompile()
        {
            throw new NotImplementedException();
        }
    }
}
