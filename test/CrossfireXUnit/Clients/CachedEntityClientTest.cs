// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crossfire.Extensions;
using Crossfire.Model.Metadata;
using CrossfireXUnit.Local;
using FluentAssertions;
using Xunit;

namespace CrossfireXUnit.Clients
{
    /// <summary>
    /// Tests for <see cref="CachedEntityClient{T}"/> (string and <see cref="UserConnectionInfo"/> values).
    /// </summary>
    public class CachedEntityClientTest : StorageTest
    {
        /// <summary>
        /// Test cases for client.Set() method.
        /// </summary>
        public static readonly List<object[]> SetConnectionInfoTestCases = new List<object[]>()
        {
            new object[]
            {
                new UserConnectionInfo[]
                {
                    new UserConnectionInfo
                    {
                        Database = "test1",
                        AzureRegion = "region1",
                        Server = "server1",
                        AzureResourceGroup = "rg1",
                        Model = "model1",
                    },
                    new UserConnectionInfo
                    {
                        Database = "test2",
                        AzureRegion = "region1",
                        Server = "server1",
                        AzureResourceGroup = "rg1",
                        Model = "model1",
                    },
                }, "test-key", "test-session-1", 3300, "setconnectioninfotest",
            },
        };

        /// <summary>
        /// Tests for Set with T = string.
        /// </summary>
        /// <param name="cacheKey">A cache key.</param>
        /// <param name="sessionId">Unique identifier.</param>
        /// <param name="cacheValue">Any string value.</param>
        /// <param name="ttl">TTL in seconds.</param>
        /// <param name="tableName">Cache table name.</param>
        /// <returns>XUnit test task.</returns>
        [Theory]
        [InlineData("test-key", "test-session", "test-value", 3600, "settest")]
        public async Task SetString(string cacheKey, string sessionId, string cacheValue, int ttl, string tableName)
        {
            await this.TestStorageContext.CreateTable(tableName);
            var client = new CachedEntityClient<string>(this.TestStorageContext, tableName);
            await client.Set(cacheKey, sessionId, cacheValue, ttl);
            var cached = await client.Get(cacheKey);
            Assert.Equal(cached.CachedValue, cacheValue);
        }

        /// <summary>
        /// Tests Get with T = string and multiple entries for the same key.
        /// </summary>
        /// <param name="cacheKey">A cache key.</param>
        /// <param name="sessionIds">Unique identifiers for each cache entry.</param>
        /// <param name="cacheValue">Any string value.</param>
        /// <param name="ttl">TTL in seconds.</param>
        /// <param name="tableName">Cache table name.</param>
        /// <returns>XUnit test task.</returns>
        [Theory]
        [InlineData("test-key", new string[] { "test-session-1", "test-session-2" }, "test-value", 3600, "getalltest")]
        public async Task GetAllString(string cacheKey, string[] sessionIds, string cacheValue, int ttl, string tableName)
        {
            await this.TestStorageContext.CreateTable(tableName);
            var client = new CachedEntityClient<string>(this.TestStorageContext, tableName);
            foreach (string sessionId in sessionIds)
            {
                await client.Set(cacheKey, sessionId, cacheValue, ttl);
            }

            string[] result = (await client.GetAll(cacheKey)).Select(v => v.CachedValue).ToArray();
            Assert.All(result, (item) =>
            {
                Assert.Equal(item, cacheValue);
            });
        }

        /// <summary>
        /// Tests Set with T = <see cref="UserConnectionInfo"/>.
        /// </summary>
        /// <param name="cacheValue">A cached value.</param>
        /// <param name="cacheKey">A cache key.</param>
        /// <param name="sessionId">A unique identifier.</param>
        /// <param name="ttl">TTL in seconds.</param>
        /// <param name="tableName">Cache table name.</param>
        /// <returns>XUnit test task.</returns>
        [Theory]
        [MemberData(nameof(SetConnectionInfoTestCases))]
        public async Task SetUserConnectionInfo(UserConnectionInfo[] cacheValue, string cacheKey, string sessionId, int ttl, string tableName)
        {
            await this.TestStorageContext.CreateTable(tableName);
            var client = new CachedEntityClient<UserConnectionInfo[]>(this.TestStorageContext, tableName);
            await client.Set(cacheKey, sessionId, cacheValue, ttl);
            var cached = await client.Get(cacheKey);
            cached.CachedValue.Should().BeEquivalentTo(cacheValue);
        }
    }
}
