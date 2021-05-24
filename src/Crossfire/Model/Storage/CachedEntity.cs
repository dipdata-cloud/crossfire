// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Crossfire.Model.Storage
{
    /// <summary>
    /// A record in a distributed cache.
    /// </summary>
    /// <typeparam name="T">Type of a cached value.</typeparam>
    public class CachedEntity<T> : TableEntity
    {
        /// <summary>
        /// Gets cache key. Multiple values can be specified for the key.
        /// </summary>
        public string CacheGroup => this.PartitionKey;

        /// <summary>
        /// Gets cache entry identifier.
        /// </summary>
        public string SessionId => this.RowKey;

        /// <summary>
        /// Gets or sets cache entry time to live in seconds.
        /// </summary>
        public int TTL { get; set; }

        /// <summary>
        /// Gets or sets a cached value.
        /// </summary>
        public T CachedValue { get; set; }

        /// <summary>
        /// Creates a cached entity for a value.
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="sessionId">Cache entry identifier.</param>
        /// <param name="value">Value to cache.</param>
        /// <param name="ttl">Time to live in seconds.</param>
        /// <returns>An instance of <see cref="CachedEntity{T}"/>.</returns>
        public static CachedEntity<T> Create(string cacheKey, string sessionId, T value, int ttl = 3300)
        {
            return new CachedEntity<T>
            {
                PartitionKey = cacheKey,
                Timestamp = DateTimeOffset.UtcNow,
                RowKey = sessionId,
                TTL = ttl,
                CachedValue = value,
            };
        }

        /// <summary>
        /// Gets expiration status for this cache entry.
        /// </summary>
        /// <returns>True of expired.</returns>
        public bool IsExpired() => DateTime.UtcNow.Subtract(this.Timestamp.UtcDateTime).TotalSeconds > this.TTL;

        /// <inheritdoc/>
        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            IDictionary<string, EntityProperty> baseEntity = base.WriteEntity(operationContext);
            if (typeof(T) != typeof(string))
            {
                baseEntity.Add(nameof(this.CachedValue), new EntityProperty(JsonConvert.SerializeObject(this.CachedValue, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                })));
            }

            return baseEntity;
        }

        /// <inheritdoc/>
        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);
            if (typeof(T) != typeof(string))
            {
                this.CachedValue = JsonConvert.DeserializeObject<T>(properties[nameof(this.CachedValue)].StringValue);
            }
            else
            {
                this.CachedValue = (T)properties[nameof(this.CachedValue)].PropertyAsObject;
            }
        }
    }
}
