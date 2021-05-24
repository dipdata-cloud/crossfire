// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Crossfire.Extensions;
using Crossfire.Model;
using CrossfireXUnit.Local;
using Newtonsoft.Json;
using Xunit;
using static Crossfire.Model.ModelConnection;

namespace CrossfireXUnit.Clients
{
    /// <summary>
    /// Tests Azure AS client.
    /// </summary>
    public class ModelConnectionTest : ParametizedTest
    {
        /// <summary>
        /// Various MDX queries to test.
        /// </summary>
        public static readonly List<object[]> ExecuteQueryTestCases = new List<object[]>
        {
            new object[]
            {
                "SELECT NON EMPTY { [Measures].[Internet Total Sales] } ON 0, NON EMPTY { ([Date].[Fiscal Year].[Fiscal Year].ALLMEMBERS ) } ON 1 FROM [Model]",
                OutputFormat.Dictionary,
                new Func<string, bool>((v) =>
                {
                    try
                    {
                        var deser = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(v);
                        if (deser.Count > 0 && deser.ContainsKey("Fiscal Year"))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }),
            },
            new object[]
            {
                "SELECT NON EMPTY { [Measures].[Internet Total Sales] } ON 0, NON EMPTY { ([Date].[Fiscal Year].[Fiscal Year].ALLMEMBERS ) } ON 1 FROM [Model]",
                OutputFormat.KeyValueArray,
                new Func<string, bool>((v) =>
                {
                    try
                    {
                        var deser = JsonConvert.DeserializeObject<KeyValuePair<string, string>[]>(v);
                        if (deser.Length > 0 && deser.Where(d => d.Key == "2010").Any())
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }),
            },
            new object[]
            {
                "SELECT NON EMPTY { [Measures].[Internet Total Sales] } ON 0, NON EMPTY { ([Date].[Fiscal Year].[Fiscal Year].ALLMEMBERS ) } ON 1 FROM [Model]",
                OutputFormat.Table,
                new Func<string, bool>((v) =>
                {
                    try
                    {
                        var deser = JsonConvert.DeserializeObject<DataTable>(v);
                        if (deser.Rows.Count > 0 && deser.Rows.Cast<DataRow>().Where(dr => dr["Fiscal Year"].ToString() == "2010").Any())
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }),
            },
            new object[]
            {
                "SELECT NON EMPTY { [Measures].[Internet Total Sales] } ON 0, NON EMPTY { ([Date].[Fiscal Year].[Fiscal Year].ALLMEMBERS ) } ON 1 FROM [Model]",
                OutputFormat.TwoDimensionalArray,
                new Func<string, bool>((v) =>
                {
                    try
                    {
                        var deser = JsonConvert.DeserializeObject<string[][]>(v);
                        if (deser.Length > 0 && deser[0].SequenceEqual(new string[] { "Fiscal Year", "Internet Total Sales" }))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }),
            },
            new object[]
            {
                "SELECT NON EMPTY { [Measures].[Internet Total Sales] } ON 0, NON EMPTY { ([Date].[Fiscal Year].[Fiscal Year].ALLMEMBERS * [Product].[Product Line].[Product Line].ALLMEMBERS ) } ON 1 FROM [Model]",
                OutputFormat.SimpleUnpivotedTable,
                new Func<string, bool>((v) =>
                {
                    try
                    {
                        var deser = JsonConvert.DeserializeObject<DataTable>(v);
                        if (deser.Columns.Count > 3)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }),
            },
        };

        /// <summary>
        /// Tests <see cref="ModelConnection.RetrieveMetadataAsync(string)"/>.
        /// </summary>
        /// <returns>XUnit test task.</returns>
        [Fact]
        public async Task RetrieveMetadata()
        {
            var modelConnection = new ModelConnection(this.GetConnectionString(await this.GetToken()));
            var metadata = await modelConnection.RetrieveMetadataAsync("adventureworks");
            Assert.True(metadata.Dimensions.Length > 0 && metadata.Measures.Length > 0, "Either Dimensions or Measures array is empty, which cannot be true for Adventureworks");
        }

        /// <summary>
        /// Tests <see cref="ModelConnection.ExecuteQueryAsync(string)"/>.
        /// </summary>
        /// <param name="query">An MDX query string.</param>
        /// <param name="outputFormat"><see cref="ModelConnection.OutputFormat"/> to apply.</param>
        /// <param name="deserializationVerifier">Function that verifies the response.</param>
        /// <returns>XUnit test task.</returns>
        [Theory]
        [MemberData(nameof(ExecuteQueryTestCases))]
        public async Task ExecuteQuery(string query, OutputFormat outputFormat, Func<string, bool> deserializationVerifier)
        {
            var modelConnection = new ModelConnection(this.GetConnectionString(await this.GetToken()), outputFormat);
            var result = await modelConnection.ExecuteQueryAsync(query);
            Assert.True(deserializationVerifier(result), $"Failed to verify output format: {outputFormat}");
        }

        private string GetConnectionString(string token) => $"{this.TestConfiguration.AzureAS.ConnectionProvider};User ID=;Password={token};Persist Security Info=True;Impersonation Level=Impersonate;Initial Catalog=adventureworks;";

        private async Task<string> GetToken()
        {
            var clientSecret = await IdentityManager.GetSecret(this.TestConfiguration.KeyVault.Uri, this.TestConfiguration.AzureAS.ApplicationId, this.TestConfiguration.Tenant);
            return IdentityManager.SecureStringToString(await IdentityManager.GetServiceBearerToken(
                tenantId: this.TestConfiguration.Tenant,
                clientId: this.TestConfiguration.AzureAS.ApplicationId,
                clientSecret: clientSecret,
                serviceUri: this.TestConfiguration.AzureAS.ServerUri));
        }
    }
}
