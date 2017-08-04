// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.EntityFrameworkCore.ReverseEngineering
{
    public abstract class E2ETestBase<TFixture> : IClassFixture<TFixture>
        where TFixture : E2EFixture, new()
    {
        private readonly ITestOutputHelper _output;

        protected E2ETestBase(TFixture fixture, ITestOutputHelper output)
        {
            _output = output;
            Fixture = fixture;
            fixture.Reporter.Clear();
        }

        protected TFixture Fixture { get; }
        protected TestOperationReporter Reporter => Fixture.Reporter;
        protected InMemoryFileService InMemoryFiles => Fixture.InMemoryFiles;
        protected IModelScaffolder Generator => Fixture.Generator;
        protected IScaffoldingModelFactory ScaffoldingModelFactory => Fixture.ScaffoldingModelFactory;
        protected abstract ICollection<BuildReference> References { get; }

        protected virtual void AssertEqualFileContents(FileSet expected, FileSet actual)
        {
            Assert.Equal(expected.Files.Count, actual.Files.Count);

            for (var i = 0; i < expected.Files.Count; i++)
            {
                Assert.True(actual.Exists(i), $"Could not find file '{actual.Files[i]}' in directory '{actual.Directory}'");
                var expectedContents = expected.Contents(i);
                var actualContents = actual.Contents(i);

                try
                {
                    Assert.Equal(expectedContents, actualContents, ignoreLineEndingDifferences: true);
                }
                catch (EqualException e)
                {
                    var sep = new string('=', 60);
                    _output.WriteLine($"Contents of actual: '{actual.Files[i]}'");
                    _output.WriteLine(sep);
                    _output.WriteLine(actualContents);
                    _output.WriteLine(sep);

                    throw new XunitException($"Files did not match: '{expected.Files[i]}' and '{actual.Files[i]}'" + Environment.NewLine + $"{e.Message}");
                }
            }
        }

        protected virtual void AssertCompile(FileSet fileSet)
        {
            var fileContents = fileSet.Files.Select(fileSet.Contents).ToList();

            var source = new BuildSource
            {
                Sources = fileContents
            };
            foreach (var r in References)
            {
                source.References.Add(r);
            }
            source.BuildInMemory();
        }
    }
}
