// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Crossfire.Storage;
using Microsoft.Extensions.Logging;

namespace CrossfireXUnit.Local
{
    /// <summary>
    /// Base class for tests that need Azure Storage API access.
    /// </summary>
    public abstract class StorageTest : ParametizedTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageTest"/> class.
        /// </summary>
        protected StorageTest()
            : base()
        {
            using var logFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = logFactory.CreateLogger<StorageContext>();
            this.TestStorageContext = new StorageContext(this.TestConfiguration.StorageAccountConnectionString, logger);
        }

        /// <summary>
        /// Gets Azure Storage context to be used by tests: Azurite, dev account etc.
        /// </summary>
        protected StorageContext TestStorageContext { get; }
    }
}
