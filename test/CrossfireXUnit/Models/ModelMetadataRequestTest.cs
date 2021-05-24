// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Crossfire.Model.Metadata;
using Xunit;

namespace CrossfireXUnit.Models
{
    /// <summary>
    /// Tests for <see cref="ModelMetadataRequest"/>.
    /// </summary>
    public class ModelMetadataRequestTest
    {
        /// <summary>
        /// Test cases for GetSHA256.
        /// </summary>
        public static readonly List<object[]> SHA256TestCases = new List<object[]>()
        {
            new object[]
            {
                new ModelMetadataRequest
                {
                    UniqueClientIdentifier = "test-id-1",
                    Region = "test-region",
                    TargetDatabase = "unit-test",
                    TargetServer = "unit-test",
                    ResourceGroup = "unit-test",
                    JobExecutorSHA = "test",
                }, "i6dEA+ol9kiR6I8xp8vL1FY5HaxBmrPQcviZj+mljeU=",
            },

            new object[]
            {
                new ModelMetadataRequest
                {
                    UniqueClientIdentifier = "test-id-2",
                    Region = "test-region",
                    TargetDatabase = "unit-test",
                    TargetServer = "unit-test",
                    ResourceGroup = "unit-test",
                    JobExecutorSHA = "test",
                }, "i6dEA+ol9kiR6I8xp8vL1FY5HaxBmrPQcviZj+mljeU=",
            },
            new object[]
            {
                new ModelMetadataRequest
                {
                    UniqueClientIdentifier = "test-id-2",
                    Region = "test-region",
                    TargetDatabase = "unit-test",
                    TargetServer = "unit-test-1",
                    ResourceGroup = "unit-test",
                    JobExecutorSHA = "test",
                }, "SR+2JVrFsVXOeNyKJYj/0FswedW1hI4TCXQHbIJEJqc=",
            },
        };

        /// <summary>
        /// Tests <see cref="ModelMetadataRequest.GetSHA256"/>.
        /// </summary>
        /// <param name="request">Request to test.</param>
        /// <param name="expectedSHA">Expected value.</param>
        [Theory]
        [MemberData(nameof(SHA256TestCases))]
        public void GetSHA256(ModelMetadataRequest request, string expectedSHA)
        {
            Assert.Equal(request.GetSHA256(), expectedSHA);
        }
    }
}
