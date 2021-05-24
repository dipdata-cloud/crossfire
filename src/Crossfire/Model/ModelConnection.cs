// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Crossfire.Model.Metadata;
using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.AnalysisServices.Tabular;
using Newtonsoft.Json;

namespace Crossfire.Model
{
    /// <summary>
    /// Isolated logic for all AAS operations.
    /// </summary>
    public class ModelConnection
    {
        /// <summary>
        /// Response content if a query results in an empty cellset.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string EMPTY_RESPONSE = "{'empty':'no data'}";

        private readonly string connectionString;
        private readonly OutputFormat format;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelConnection"/> class.
        /// </summary>
        public ModelConnection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelConnection"/> class.
        /// </summary>
        /// <param name="connectionString">Azure AS connection string.</param>
        /// <param name="format"><see cref="OutputFormat"/> for all queries using this connection.</param>
        public ModelConnection(string connectionString, OutputFormat format)
        {
            this.connectionString = connectionString;
            this.format = format;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelConnection"/> class.
        /// </summary>
        /// <param name="connectionString">Azure AS connection string.</param>
        public ModelConnection(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Supported response formats.
        /// </summary>
        public enum OutputFormat
        {
            /// <summary>
            /// Returns JSON-serialized <see cref="DataTable"/>.
            /// </summary>
            Table = 0,

            /// <summary>
            /// Returns JSON-serialized <see cref="Dictionary{TKey, TValue}"/> where key is string (column) and value is an array.
            /// </summary>
            Dictionary = 1,

            /// <summary>
            /// Returns JSON-serialized array of <see cref="KeyValuePair"/> elements.
            /// </summary>
            KeyValueArray = 2,

            /// <summary>
            /// Returns JSON-serialized array[][] of elements.
            /// </summary>
            TwoDimensionalArray = 3,

            /// <summary>
            /// Returns JSON-serialized <see cref="DataTable"/> with second column values unpivoted.
            /// </summary>
            SimpleUnpivotedTable = 4,
        }

        /// <summary>
        /// Executes a text query (DAX or MDX) against Azure AS.
        /// </summary>
        /// <param name="query">Query text.</param>
        /// <returns>Query result.</returns>
        public async Task<string> ExecuteQueryAsync(string query)
        {
            return await Task.Run(() => this.ExecuteQuery(query, this.format));
        }

        /// <summary>
        /// Retrieves Azure AS Model metadata.
        /// </summary>
        /// <param name="databaseName">Azure AS database name.</param>
        /// <returns>An instance of <see cref="ModelMetadata"/>.</returns>
        public async Task<ModelMetadata> RetrieveMetadataAsync(string databaseName)
        {
            return await Task.Run(() => this.RetrieveMetadata(databaseName));
        }

        private ModelMetadata RetrieveMetadata(string databaseName)
        {
            ModelMetadata result = new ModelMetadata() { LastUpdated = DateTime.UtcNow };
            using (Server srv = new Server())
            {
                srv.Connect(this.connectionString);

                Database db = srv.Databases.FindByName(databaseName);
                var model = db.Model;
                result.ModelName = model.Name;

                // number of dimensions equals number of tables
                // with at least one visible attribute / column
                result.ImportDimensions(model.Tables);

                result.ImportMeasures(model.Tables);

                srv.Disconnect();
            }

            return result;
        }

        private string ExecuteQuery(string query, OutputFormat outputFormat)
        {
            using AdomdConnection con = new AdomdConnection(this.connectionString);
            con.Open();
            AdomdCommand cmd = new AdomdCommand(query, con);
            object result = null;
            AdomdDataReader cmdResult = null;

            switch (outputFormat)
            {
                case OutputFormat.Dictionary:
                    try
                    {
                        cmdResult = cmd.ExecuteReader();
                        result = this.ReaderToDictionary(cmdResult);
                    }
                    finally
                    {
                        if (cmdResult == null)
                        {
                            result = EMPTY_RESPONSE;
                        }

                        con.Close();
                    }

                    return JsonConvert.SerializeObject(result, Formatting.Indented);

                case OutputFormat.Table:
                    try
                    {
                        cmdResult = cmd.ExecuteReader();
                        result = this.ReaderToDataTable(cmdResult);
                        con.Close();
                    }
                    finally
                    {
                        if (cmdResult == null)
                        {
                            result = EMPTY_RESPONSE;
                        }

                        con.Close();
                    }

                    return JsonConvert.SerializeObject(result, Formatting.Indented);

                case OutputFormat.KeyValueArray:
                    try
                    {
                        cmdResult = cmd.ExecuteReader();
                        result = this.ReaderKeyValueArray(cmdResult);
                        con.Close();
                    }
                    finally
                    {
                        if (cmdResult == null)
                        {
                            result = EMPTY_RESPONSE;
                        }

                        con.Close();
                    }

                    return JsonConvert.SerializeObject(result, Formatting.Indented);

                case OutputFormat.TwoDimensionalArray:
                    try
                    {
                        cmdResult = cmd.ExecuteReader();
                        result = this.ReaderToTwoDimensionalArray(cmdResult);
                        con.Close();
                    }
                    finally
                    {
                        if (cmdResult == null)
                        {
                            result = EMPTY_RESPONSE;
                        }

                        con.Close();
                    }

                    return JsonConvert.SerializeObject(result, Formatting.Indented);

                case OutputFormat.SimpleUnpivotedTable:
                    try
                    {
                        cmdResult = cmd.ExecuteReader();
                        result = this.ReaderToDataTable(cmdResult, true);
                        con.Close();
                    }
                    finally
                    {
                        if (cmdResult == null)
                        {
                            result = EMPTY_RESPONSE;
                        }

                        con.Close();
                    }

                    return JsonConvert.SerializeObject(result, Formatting.Indented);

                default:
                    return EMPTY_RESPONSE;
            }
        }

        private string ExtractColumnName(string columnName)
        {
            string output_name = columnName
                    .Replace("[Measures].", string.Empty)
                    .Replace(".[MEMBER_CAPTION]", string.Empty)
                    .Replace("[", string.Empty)
                    .Replace("]", string.Empty);

            var column_names = output_name.Split('.').Where(coln => !coln.Contains("&")).ToArray();
            return column_names[^1];
        }

        /// <summary>
        /// Converts MDX or SQL query result to dictionary.
        /// </summary>
        /// <param name="queryResult">ADO.NET data reader.</param>
        /// <returns>Query result convert to <see cref="OutputFormat.Dictionary"/>.</returns>
        private Dictionary<string, List<string>> ReaderToDictionary<T>(T queryResult)
            where T : IDataReader
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();

            // get columns from reader
            int columns = queryResult.FieldCount;
            string[] columnList = new string[columns];

            for (int col = 0; col < columns; ++col)
            {
                string column_name = this.ExtractColumnName(queryResult.GetName(col));
                result.Add(column_name, new List<string>());
                columnList[col] = column_name;
            }

            // fill datatale
            while (queryResult.Read())
            {
                for (int colIx = 0; colIx < columns; ++colIx)
                {
                    if (queryResult[colIx] != null)
                    {
                        result[columnList[colIx]].Add(queryResult[colIx].ToString());
                    }
                    else
                    {
                        result[columnList[colIx]].Add(null);
                    }
                }
            }

            queryResult.Close();

            return result;
        }

        /// <summary>
        /// Converts MDX or SQL query result to datatable.
        /// </summary>
        /// <param name="queryResult">ADO.NET data reader.</param>
        /// <returns>Query result convert to <see cref="OutputFormat.Table"/> or <see cref="OutputFormat.SimpleUnpivotedTable"/>.</returns>
        private DataTable ReaderToDataTable<T>(T queryResult, bool unPivot = false)
            where T : IDataReader
        {
            DataTable result = new DataTable();

            if (!unPivot)
            {
                // get columns from reader
                int columns = queryResult.FieldCount;

                for (int col = 0; col < columns; ++col)
                {
                    string column_name = this.ExtractColumnName(queryResult.GetName(col));
                    result.Columns.Add(column_name);
                }

                // fill datatale
                while (queryResult.Read())
                {
                    var rw = result.NewRow();
                    for (int coliX = 0; coliX < columns; ++coliX)
                    {
                        rw[coliX] = queryResult[coliX];
                    }

                    result.Rows.Add(rw);
                }
            }
            else
            {
                if (unPivot && queryResult.FieldCount != 3)
                {
                    throw new ArgumentException("Simple unpivot can only be performed for a table with 3 columns: column with keys, column with pivoted values and column with values");
                }
                else
                {
                    // 0 - key column
                    // 1 - pivoted column
                    // 2 - value column
                    string keyColumnName = this.ExtractColumnName(queryResult.GetName(0));
                    result.Columns.Add(keyColumnName);

                    Dictionary<Tuple<string, string>, string> values = new Dictionary<Tuple<string, string>, string>();
                    while (queryResult.Read())
                    {
                        Tuple<string, string> unpivotedValues = new Tuple<string, string>(queryResult[0].ToString(), queryResult[1].ToString());
                        values.Add(unpivotedValues, queryResult[2].ToString());
                    }

                    foreach (string unpivoted in values.Keys.Select(key => key.Item2).Distinct())
                    {
                        result.Columns.Add(unpivoted);
                    }

                    foreach (var unpivotedValue in values.Keys.Select(key => key.Item1).Distinct())
                    {
                        var rw = result.NewRow();
                        rw[0] = unpivotedValue;
                        foreach (var pivotKV in values.Where(v => v.Key.Item1 == unpivotedValue))
                        {
                            rw[rw.Table.Columns.IndexOf(pivotKV.Key.Item2)] = values[pivotKV.Key];
                        }

                        result.Rows.Add(rw);
                    }
                }
            }

            queryResult.Close();

            return result;
        }

        /// <summary>
        /// Converts MDX or SQL query result to KV array. This will ignore any data in the output query except for the two first columns.
        /// </summary>
        /// <param name="queryResult">ADO.NET data reader.</param>
        /// <returns>Query result convert to <see cref="OutputFormat.KeyValueArray"/>.</returns>
        private KeyValuePair<string, string>[] ReaderKeyValueArray<T>(T queryResult)
            where T : IDataReader
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            // fill k-v list
            while (queryResult.Read())
            {
                KeyValuePair<string, string> kv = new KeyValuePair<string, string>(
                    key: queryResult[0]?.ToString(),
                    value: queryResult[1]?.ToString());
                result.Add(kv);
            }

            queryResult.Close();

            return result.ToArray();
        }

        /// <summary>
        /// Converts MDX or SQL query _result to list of string arrays. Grouping by series should be handled on client-side.
        /// </summary>
        /// <param name="queryResult">ADO.NET data reader.</param>
        /// <returns>Query result convert to <see cref="OutputFormat.TwoDimensionalArray"/>.</returns>
        private string[][] ReaderToTwoDimensionalArray<T>(T queryResult)
            where T : IDataReader
        {
            List<string[]> result = new List<string[]>();

            // get columns from reader
            int columns = queryResult.FieldCount;
            string[] columnList = new string[columns];

            for (int col = 0; col < columns; ++col)
            {
                string column_name = this.ExtractColumnName(queryResult.GetName(col));
                columnList[col] = column_name;
            }

            // append header as a separate row
            result.Add(columnList);

            // fill 2d dictionary
            while (queryResult.Read())
            {
                string[] row = new string[queryResult.FieldCount];
                for (int ixRow = 0; ixRow < row.Length; ++ixRow)
                {
                    if (queryResult[ixRow] != null)
                    {
                        row[ixRow] = queryResult[ixRow].ToString();
                    }
                    else
                    {
                        row[ixRow] = null;
                    }
                }

                result.Add(row);
            }

            queryResult.Close();

            return result.ToArray();
        }
    }
}
