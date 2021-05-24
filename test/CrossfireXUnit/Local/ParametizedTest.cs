// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using Newtonsoft.Json;

namespace CrossfireXUnit.Local
{
    /// <summary>
    /// Base class for tests that require usage of testsettings.json.
    /// </summary>
    public abstract class ParametizedTest : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParametizedTest"/> class.
        /// </summary>
        protected ParametizedTest()
        {
            this.TestConfiguration = JsonConvert.DeserializeObject<TestConfiguration>(File.ReadAllText("./testsettings.json"));
        }

        /// <summary>
        /// Gets configuration for tests that require usage of testsettings.json.
        /// </summary>
        public TestConfiguration TestConfiguration { get; }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
        }
    }
}
