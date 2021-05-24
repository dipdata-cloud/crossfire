// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;

namespace Crossfire.Storage
{
    /// <summary>
    /// Class for storage operations.
    /// </summary>
    public sealed class StorageContext
    {
        private readonly CloudStorageAccount storageAccount;
        private readonly ILogger<StorageContext> logger;
        private readonly CloudTableClient tableClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageContext"/> class.
        /// </summary>
        /// <param name="connectionString">Azure Storage connection string.</param>
        /// <param name="logger">Logger instance.</param>
        public StorageContext(string connectionString, ILogger<StorageContext> logger)
        {
            this.storageAccount = CloudStorageAccount.Parse(connectionString);
            this.logger = logger;
            this.tableClient = this.storageAccount.CreateCloudTableClient();
        }

        /// <summary>
        /// Creates or updates a record in an Azure Table.
        /// </summary>
        /// <typeparam name="T">Type of an entity.</typeparam>
        /// <param name="entity">Record instance.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="replace">Whether to replace or merge the record, if one exists already.</param>
        /// <returns>A task that represents ExecuteAsync operation from Cosmos Table API.</returns>
        public async Task CreateEntity<T>(T entity, string tableName, bool replace = false)
            where T : TableEntity
        {
            TableOperation update;
            if (replace)
            {
                update = TableOperation.InsertOrReplace(entity);
            }
            else
            {
                update = TableOperation.InsertOrMerge(entity);
            }

            await this.ExecuteOperation(tableName, update);
        }

        /// <summary>
        /// Creates a table in Azure Table Storage.
        /// </summary>
        /// <param name="tableName">Name of a table.</param>
        /// <returns>A task representing CreateIfNotExistsAsync operation from Cosmos Table API.</returns>
        public async Task CreateTable(string tableName)
        {
            CloudTable cloudTable = this.tableClient.GetTableReference(tableName);
            await cloudTable.CreateIfNotExistsAsync();
        }

        /// <summary>
        /// Extracts all records with matching Partition Key. If Partition Key is empty, extracts all records.
        /// </summary>
        /// <typeparam name="T">Entity to extract.</typeparam>
        /// <param name="partitionKey">Partition Key filter value or empty string.</param>
        /// <param name="tableName">Table to search in.</param>
        /// <returns>Array of T.</returns>
        public async Task<T[]> FindAll<T>(string partitionKey, string tableName)
            where T : TableEntity, new()
        {
            List<T> result = new List<T>();
            CloudTable cloudTable = this.tableClient.GetTableReference(tableName);
            TableQuery<T> query = new TableQuery<T>();
            if (partitionKey != string.Empty)
            {
                query = query.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
            }
            else
            {
                query = query.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, partitionKey));
            }

            TableContinuationToken token = null;
            do
            {
                var rows = await cloudTable.ExecuteQuerySegmentedAsync<T>(query, token);
                token = rows.ContinuationToken;

                foreach (var row in rows)
                {
                    result.Add(row);
                }
            }
            while (token != null);

            return result.ToArray();
        }

        private async Task<TableResult> ExecuteOperation(string tableName, TableOperation operation)
        {
            CloudTable cloudTable = this.tableClient.GetTableReference(tableName);
            return await cloudTable.ExecuteAsync(operation);
        }
    }
}
