// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Crossfire.Model.Storage;
using Crossfire.Storage;

namespace Crossfire.Extensions
{
    /// <summary>
    /// Distributed cache client that relies on conflict resolution on storage side.
    /// Actual implementation depends on what backend is used in <see cref="StorageContext"/> in respective methods.
    /// </summary>
    /// <typeparam name="T">Type of a cache value.</typeparam>
    public class CachedEntityClient<T>
    {
        private readonly StorageContext storageContext;
        private readonly string cacheTableName;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedEntityClient{T}"/> class.
        /// </summary>
        /// <param name="context">Connected storage access context.</param>
        /// <param name="cacheTableName">Cache table name.</param>
        public CachedEntityClient(StorageContext context, string cacheTableName)
        {
            this.storageContext = context;
            this.cacheTableName = cacheTableName;
        }

        /// <summary>
        /// Retrieves a first non-expired cached value from the cache.
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <returns>Cached value wrapped in <see cref="CachedEntity{T}"/>.</returns>
        public async Task<CachedEntity<T>> Get(string cacheKey)
        {
            var entities = await this.storageContext.FindAll<CachedEntity<T>>(cacheKey, this.cacheTableName);

            return entities.FirstOrDefault(t => !t.IsExpired());
        }

        /// <summary>
        /// Retrieves all non-expired cache values for the key.
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <returns>Cached values wrapped in <see cref="CachedEntity{T}"/>.</returns>
        public async Task<CachedEntity<T>[]> GetAll(string cacheKey)
        {
            var entities = await this.storageContext.FindAll<CachedEntity<T>>(cacheKey, this.cacheTableName);

            return entities.Where(t => !t.IsExpired()).ToArray();
        }

        /// <summary>
        /// Saves a key-value pair to the cache.
        /// </summary>
        /// <param name="cacheKey">Unique key identifying the value.</param>
        /// <param name="sessionId">Application session that sets the value, or any other unique identifier.</param>
        /// <param name="cacheValue">A value to save.</param>
        /// <param name="ttl">Time To Live for the saved value, in seconds.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous saving to the cache.</returns>
        public async Task Set(string cacheKey, string sessionId, T cacheValue, int ttl = 3300)
        {
            var entity = CachedEntity<T>.Create(cacheKey, sessionId, cacheValue, ttl);
            await this.storageContext.CreateEntity(entity, this.cacheTableName);
        }
    }
}
