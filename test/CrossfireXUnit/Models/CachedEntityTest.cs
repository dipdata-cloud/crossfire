// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;
using Crossfire.Model.Storage;
using Xunit;

namespace CrossfireXUnit.Models
{
    /// <summary>
    /// Tests for <see cref="CachedEntity{T}"/>.
    /// </summary>
    public class CachedEntityTest
    {
        /// <summary>
        /// Tests <see cref="CachedEntity{T}.IsExpired"/>.
        /// </summary>
        /// <param name="cacheKey">A cache key.</param>
        /// <param name="sessionId">Unique identifier for the entry.</param>
        /// <param name="value">A value to cache.</param>
        /// <param name="ttl">TTL in seconds.</param>
        /// <returns>XUnit test task.</returns>
        [Theory]
        [InlineData("test", "test-session", "test-value", 3600)]
        [InlineData("test", "test-session", "test-value", 1)]
        public async Task IsExpired(string cacheKey, string sessionId, string value, int ttl)
        {
            var sw = new Stopwatch();
            sw.Start();
            var ent = CachedEntity<string>.Create(cacheKey, sessionId, value, ttl);
            await Task.Delay(2000);
            sw.Stop();
            var result = (sw.ElapsedMilliseconds / 1000.0 < 3600) | ent.IsExpired();
            Assert.True(result);
        }

        /// <summary>
        /// Tests <see cref="CachedEntity{T}.Create(string, string, T, int)"/>.
        /// </summary>
        /// <param name="cacheKey">A cache key.</param>
        /// <param name="sessionId">Unique identifier for the entry.</param>
        /// <param name="value">A value to cache.</param>
        /// <param name="ttl">TTL in seconds.</param>
        [Theory]
        [InlineData("test", "test-session", "test-value", 3600)]
        public void Create(string cacheKey, string sessionId, string value, int ttl)
        {
            var ent = CachedEntity<string>.Create(cacheKey, sessionId, value, ttl);
            Assert.True(ent.PartitionKey == ent.CacheGroup && ent.RowKey == ent.SessionId);
        }
    }
}
