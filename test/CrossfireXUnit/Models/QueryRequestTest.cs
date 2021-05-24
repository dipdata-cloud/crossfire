// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Crossfire.Model.Query;
using Xunit;

namespace CrossfireXUnit.Models
{
    /// <summary>
    /// Tests for <see cref="QueryRequest"/>.
    /// </summary>
    public class QueryRequestTest
    {
        /// <summary>
        /// Test cases for GetSHA256.
        /// </summary>
        public static readonly List<object[]> SHA256TestCases = new List<object[]>()
        {
            new object[]
            {
                new QueryRequest
                {
                    CompilationTarget = CompilationTargets.MDX,
                    QueryFilters = new string[] { "[Product].[Product].&[Apple]" },
                    QueryValues = new string[] { "[Measures].[Sales Amount]" },
                    UniqueClientIdentifier = "test-id",
                    Region = "test-region",
                    TargetDatabase = "unit-test",
                    TargetServer = "unit-test",
                    ResourceGroup = "unit-test",
                    ModelName = "test-model",
                    JobExecutorSHA = "test",
                }, "3LXZQhmzr+5jGSwq3bhJ0RK5wDpVf+kR2fHMxUMG/lk=",
            },

            new object[]
            {
                new QueryRequest
                {
                    CompilationTarget = CompilationTargets.MDX,
                    QueryValues = new string[] { "[Measures].[Sales Amount]" },
                    UniqueClientIdentifier = "test-id",
                    Region = "test-region",
                    TargetDatabase = "unit-test",
                    TargetServer = "unit-test",
                    ResourceGroup = "unit-test",
                    ModelName = "test-model",
                    JobExecutorSHA = "test",
                }, "UZOodEDCR9h2uT5HB/gJoEJBYlOiVyoBRfQ4sY/2c4w=",
            },

            new object[]
            {
                new QueryRequest
                {
                    CompilationTarget = CompilationTargets.MDX,
                    QueryFilters = new string[] { "[Product].[Product].&[Apple]" },
                    UniqueClientIdentifier = "test-id",
                    Region = "test-region",
                    TargetDatabase = "unit-test",
                    TargetServer = "unit-test",
                    ResourceGroup = "unit-test",
                    ModelName = "test-model",
                    JobExecutorSHA = "test",
                }, "fail",
            },
        };

        /// <summary>
        /// Test cases for <see cref="QueryRequest.Compile"/> (MDX).
        /// </summary>
        public static readonly List<object[]> MDXCompileTestCases = new List<object[]>()
        {
            new object[]
            {
                new QueryRequest
                {
                    CompilationTarget = CompilationTargets.MDX,
                    QueryValues = new string[] { "[Measures].[Sales Amount]", "[Measures].[Sales Count]" },
                    UniqueClientIdentifier = "test-id",
                    Region = "test-region",
                    TargetDatabase = "unit-test",
                    TargetServer = "unit-test",
                    ResourceGroup = "unit-test",
                    ModelName = "test-model",
                    JobExecutorSHA = "test",
                }, " select non empty { [Measures].[Sales Amount], [Measures].[Sales Count] } on 0 from ( select  from [test-model] )",
            },

            new object[]
            {
                new QueryRequest
                {
                    CompilationTarget = CompilationTargets.MDX,
                    QueryFilters = new string[] { "[Product].[Product].&[Apple]" },
                    QueryValues = new string[] { "[Measures].[Sales Amount]" },
                    UniqueClientIdentifier = "test-id",
                    Region = "test-region",
                    TargetDatabase = "unit-test",
                    TargetServer = "unit-test",
                    ResourceGroup = "unit-test",
                    ModelName = "test-model",
                    JobExecutorSHA = "test",
                }, " select non empty { [Measures].[Sales Amount] } on 0 from ( select [Product].[Product].&[Apple] on 0 from [test-model] )",
            },

            new object[]
            {
                new QueryRequest
                {
                    CompilationTarget = CompilationTargets.MDX,
                    QuerySlices = new string[] { "[Date].[Year].[All].children" },
                    QueryFilters = new string[] { "[Product].[Product].&[Apple]" },
                    QueryValues = new string[] { "[Measures].[Sales Amount]" },
                    UniqueClientIdentifier = "test-id",
                    Region = "test-region",
                    TargetDatabase = "unit-test",
                    TargetServer = "unit-test",
                    ResourceGroup = "unit-test",
                    ModelName = "test-model",
                    JobExecutorSHA = "test",
                }, " select non empty { [Measures].[Sales Amount] } on 0, ([Date].[Year].[All].children) on 1 from ( select [Product].[Product].&[Apple] on 0 from [test-model] )",
            },

            new object[]
            {
                new QueryRequest
                {
                    CompilationTarget = CompilationTargets.MDX,
                    QuerySlices = new string[] { "[Date].[Year].[All].children" },
                    QueryFilters = new string[] { "[Product].[Product].&[Apple]" },
                    QueryValues = new string[] { "[Measures].[Sales Amount]" },
                    CustomSets = new string[] { "test as { [Date].[Date].&[20200101] : [Date].[Date].&[20200301] }" },
                    UniqueClientIdentifier = "test-id",
                    Region = "test-region",
                    TargetDatabase = "unit-test",
                    TargetServer = "unit-test",
                    ResourceGroup = "unit-test",
                    ModelName = "test-model",
                    JobExecutorSHA = "test",
                }, $"with {Environment.NewLine}set test as {{ [Date].[Date].&[20200101] : [Date].[Date].&[20200301] }}{Environment.NewLine} select non empty {{ [Measures].[Sales Amount] }} on 0, ([Date].[Year].[All].children) on 1 from ( select [Product].[Product].&[Apple] on 0 from [test-model] )",
            },

            new object[]
            {
                new QueryRequest
                {
                    CompilationTarget = CompilationTargets.MDX,
                    QuerySlices = new string[] { "[Date].[Year].[All].children" },
                    QueryFilters = new string[] { "[Product].[Product].&[Apple]" },
                    QueryValues = new string[] { "[Measures].[Sales Amount]" },
                    CustomMembers = new string[] { "[Date].[Date].[Others] as \"dummy\"" },
                    UniqueClientIdentifier = "test-id",
                    Region = "test-region",
                    TargetDatabase = "unit-test",
                    TargetServer = "unit-test",
                    ResourceGroup = "unit-test",
                    ModelName = "test-model",
                    JobExecutorSHA = "test",
                }, $"with {Environment.NewLine}member [Date].[Date].[Others] as \"dummy\"{Environment.NewLine} select non empty {{ [Measures].[Sales Amount] }} on 0, ([Date].[Year].[All].children) on 1 from ( select [Product].[Product].&[Apple] on 0 from [test-model] )",
            },

            new object[]
            {
                new QueryRequest
                {
                    CompilationTarget = CompilationTargets.MDX,
                    QuerySlices = new string[] { "[Date].[Year].[All].children" },
                    QueryValues = new string[] { "[Measures].[Sales Amount]" },
                    DefaultMeasure = "[Measures].[Sales Count]",
                    UniqueClientIdentifier = "test-id",
                    Region = "test-region",
                    TargetDatabase = "unit-test",
                    TargetServer = "unit-test",
                    ResourceGroup = "unit-test",
                    ModelName = "test-model",
                    JobExecutorSHA = "test",
                }, " select non empty { [Measures].[Sales Amount] } on 0, nonempty( ([Date].[Year].[All].children), [Measures].[Sales Count]) on 1 from ( select  from [test-model] )",
            },

            new object[]
            {
                new QueryRequest
               {
                   CompilationTarget = CompilationTargets.MDX,
                   QuerySlices = new string[] { "[Date].[Year].[All].children", "[Date].[Month].[All].children" },
                   QueryValues = new string[] { "[Measures].[Sales Amount]" },
                   DefaultMeasure = "[Measures].[Sales Count]",
                   UniqueClientIdentifier = "test-id",
                   Region = "test-region",
                   TargetDatabase = "unit-test",
                   TargetServer = "unit-test",
                   ResourceGroup = "unit-test",
                   ModelName = "test-model",
                   JobExecutorSHA = "test",
               }, " select non empty { [Measures].[Sales Amount] } on 0, nonempty( ([Date].[Year].[All].children, [Date].[Month].[All].children), [Measures].[Sales Count]) on 1 from ( select  from [test-model] )",
            },
        };

        /// <summary>
        /// GetSHA256 test.
        /// </summary>
        /// <param name="request">Request to test.</param>
        /// <param name="expectedSHA">Expected value.</param>
        [Theory]
        [MemberData(nameof(SHA256TestCases))]
        public void GetSHA256(QueryRequest request, string expectedSHA)
        {
            if (request.QueryValues == null)
            {
                Assert.Throws<ArgumentNullException>(() => request.GetSHA256());
            }
            else
            {
                Assert.Equal(expectedSHA, request.GetSHA256());
            }
        }

        /// <summary>
        /// MDXCompile test.
        /// </summary>
        /// <param name="request">Request to test.</param>
        /// <param name="expectedQuery">Expected text query to be generated.</param>
        [Theory]
        [MemberData(nameof(MDXCompileTestCases))]
        public void MDXCompile(QueryRequest request, string expectedQuery)
        {
            Assert.Equal(expectedQuery, request.Compile());
        }
    }
}
