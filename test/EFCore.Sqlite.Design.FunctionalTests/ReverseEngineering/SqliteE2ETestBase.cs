// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable StringStartsWithIsCultureSpecific

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.ReverseEngineering
{
    public abstract class SqliteE2ETestBase : E2ETestBase<SqliteE2EFixture>
    {
        public readonly string TestProjectPath;
        public readonly string TestProjectFullPath;

        protected SqliteE2ETestBase(SqliteE2EFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            TestProjectPath = Path.Combine(DbSuffix, "testout");
            TestProjectFullPath = Path.GetFullPath(TestProjectPath);
        }

        [Fact]
        public virtual void One_to_one()
        {
            using (var testStore = SqliteTestStore.GetOrCreateInitialized("OneToOne" + DbSuffix))
            {
                testStore.ExecuteNonQuery(@"
CREATE TABLE Principal (
    Id INTEGER PRIMARY KEY AUTOINCREMENT
);
CREATE TABLE Dependent (
    Id INT,
    PrincipalId INT NOT NULL UNIQUE,
    PRIMARY KEY (Id),
    FOREIGN KEY (PrincipalId) REFERENCES Principal (Id)
);
");

                var results = Generator.Generate(
                    testStore.ConnectionString,
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<string>(),
                    TestProjectPath,
                    outputPath: null,
                    rootNamespace: "E2E.Sqlite",
                    contextName: null,
                    useDataAnnotations: UseDataAnnotations,
                    overwriteFiles: false,
                    useDatabaseNames: false);

                Assert.Empty(Reporter.Messages.Where(m => m.StartsWith("warn: ")));

                var expectedFileSet = new FileSet(new FileSystemFileService(), Path.Combine(ExpectedResultsParentDir, "OneToOne"))
                {
                    Files =
                    {
                        "OneToOne" + DbSuffix + "Context.cs",
                        "Dependent.cs",
                        "Principal.cs"
                    }
                };
                var actualFileSet = new FileSet(InMemoryFiles, TestProjectFullPath)
                {
                    Files = Enumerable.Repeat(results.ContextFile, 1).Concat(results.EntityTypeFiles).Select(Path.GetFileName).ToList()
                };
                AssertEqualFileContents(expectedFileSet, actualFileSet);
                AssertCompile(actualFileSet);
            }
        }

        [Fact]
        public virtual void One_to_many()
        {
            using (var testStore = SqliteTestStore.GetOrCreateInitialized("OneToMany" + DbSuffix))
            {
                testStore.ExecuteNonQuery(@"
CREATE TABLE OneToManyPrincipal (
    OneToManyPrincipalID1 INT,
    OneToManyPrincipalID2 INT,
    Other TEXT NOT NULL,
    PRIMARY KEY (OneToManyPrincipalID1, OneToManyPrincipalID2)
);
CREATE TABLE OneToManyDependent (
    OneToManyDependentID1 INT NOT NULL,
    OneToManyDependentID2 INT NOT NULL,
    SomeDependentEndColumn VARCHAR NOT NULL,
    OneToManyDependentFK1 INT,
    OneToManyDependentFK2 INT,
    PRIMARY KEY (OneToManyDependentID1, OneToManyDependentID2),
    FOREIGN KEY ( OneToManyDependentFK1, OneToManyDependentFK2)
        REFERENCES OneToManyPrincipal ( OneToManyPrincipalID1, OneToManyPrincipalID2  )
);
");

                var results = Generator.Generate(
                    testStore.ConnectionString,
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<string>(),
                    TestProjectPath,
                    outputPath: null,
                    rootNamespace: "E2E.Sqlite",
                    contextName: null,
                    useDataAnnotations: UseDataAnnotations,
                    overwriteFiles: false,
                    useDatabaseNames: false);

                Assert.Empty(Reporter.Messages.Where(m => m.StartsWith("warn: ")));

                var expectedFileSet = new FileSet(new FileSystemFileService(), Path.Combine(ExpectedResultsParentDir, "OneToMany"))
                {
                    Files =
                    {
                        "OneToMany" + DbSuffix + "Context.cs",
                        "OneToManyDependent.cs",
                        "OneToManyPrincipal.cs"
                    }
                };
                var actualFileSet = new FileSet(InMemoryFiles, TestProjectFullPath)
                {
                    Files = Enumerable.Repeat(results.ContextFile, 1).Concat(results.EntityTypeFiles).Select(Path.GetFileName).ToList()
                };
                AssertEqualFileContents(expectedFileSet, actualFileSet);
                AssertCompile(actualFileSet);
            }
        }

        [Fact]
        public virtual void Many_to_many()
        {
            using (var testStore = SqliteTestStore.GetOrCreateInitialized("ManyToMany" + DbSuffix))
            {
                testStore.ExecuteNonQuery(@"
CREATE TABLE Users ( Id PRIMARY KEY);
CREATE TABLE Groups (Id PRIMARY KEY);
CREATE TABLE Users_Groups (
    Id PRIMARY KEY,
    UserId,
    GroupId,
    UNIQUE (UserId, GroupId),
    FOREIGN KEY (UserId) REFERENCES Users (Id),
    FOREIGN KEY (GroupId) REFERENCES Groups (Id)
);
");

                var results = Generator.Generate(
                    testStore.ConnectionString,
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<string>(),
                    TestProjectPath,
                    outputPath: null,
                    rootNamespace: "E2E.Sqlite",
                    contextName: null,
                    useDataAnnotations: UseDataAnnotations,
                    overwriteFiles: false,
                    useDatabaseNames: false);

                Assert.Empty(Reporter.Messages.Where(m => m.StartsWith("warn: ")));

                var expectedFileSet = new FileSet(new FileSystemFileService(), Path.Combine(ExpectedResultsParentDir, "ManyToMany"))
                {
                    Files =
                    {
                        "ManyToMany" + DbSuffix + "Context.cs",
                        "Groups.cs",
                        "Users.cs",
                        "UsersGroups.cs"
                    }
                };
                var actualFileSet = new FileSet(InMemoryFiles, TestProjectFullPath)
                {
                    Files = Enumerable.Repeat(results.ContextFile, 1).Concat(results.EntityTypeFiles).Select(Path.GetFileName).ToList()
                };
                AssertEqualFileContents(expectedFileSet, actualFileSet);
                AssertCompile(actualFileSet);
            }
        }

        [Fact]
        public virtual void Self_referencing()
        {
            using (var testStore = SqliteTestStore.GetOrCreateInitialized("SelfRef" + DbSuffix))
            {
                testStore.ExecuteNonQuery(@"CREATE TABLE SelfRef (
    Id INTEGER PRIMARY KEY,
    SelfForeignKey INTEGER,
    FOREIGN KEY (SelfForeignKey) REFERENCES SelfRef (Id)
);");

                var results = Generator.Generate(
                    testStore.ConnectionString,
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<string>(),
                    TestProjectPath,
                    outputPath: null,
                    rootNamespace: "E2E.Sqlite",
                    contextName: null,
                    useDataAnnotations: UseDataAnnotations,
                    overwriteFiles: false,
                    useDatabaseNames: false);

                Assert.Empty(Reporter.Messages.Where(m => m.StartsWith("warn: ")));

                var expectedFileSet = new FileSet(new FileSystemFileService(), Path.Combine(ExpectedResultsParentDir, "SelfRef"))
                {
                    Files =
                    {
                        "SelfRef" + DbSuffix + "Context.cs",
                        "SelfRef.cs"
                    }
                };
                var actualFileSet = new FileSet(InMemoryFiles, TestProjectFullPath)
                {
                    Files = Enumerable.Repeat(results.ContextFile, 1).Concat(results.EntityTypeFiles).Select(Path.GetFileName).ToList()
                };
                AssertEqualFileContents(expectedFileSet, actualFileSet);
                AssertCompile(actualFileSet);
            }
        }

        [Fact]
        public virtual void Missing_primary_key()
        {
            using (var testStore = SqliteTestStore.GetOrCreateInitialized("NoPk" + DbSuffix))
            {
                testStore.ExecuteNonQuery("CREATE TABLE Alicia ( Keys TEXT );");

                var results = Generator.Generate(
                    testStore.ConnectionString,
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<string>(),
                    TestProjectPath,
                    outputPath: null,
                    rootNamespace: "E2E.Sqlite",
                    contextName: null,
                    useDataAnnotations: UseDataAnnotations,
                    overwriteFiles: false,
                    useDatabaseNames: false);

                var errorMessage = DesignStrings.UnableToGenerateEntityType("Alicia");
                Assert.Contains("warn: " + DesignStrings.MissingPrimaryKey("Alicia"), Reporter.Messages);
                Assert.Contains("warn: " + errorMessage, Reporter.Messages);
                Assert.Contains(errorMessage, InMemoryFiles.RetrieveFileContents(TestProjectFullPath, Path.GetFileName(results.ContextFile)));
            }
        }

        [Fact]
        public virtual void Principal_missing_primary_key()
        {
            using (var testStore = SqliteTestStore.GetOrCreateInitialized("NoPrincipalPk" + DbSuffix))
            {
                testStore.ExecuteNonQuery(@"CREATE TABLE DependentNoPrincipalPk (
    Id PRIMARY KEY,
    PrincipalId INT,
    FOREIGN KEY (PrincipalId) REFERENCES Principal(Id)
);
CREATE TABLE Principal ( Id INT);");

                var results = Generator.Generate(
                    testStore.ConnectionString,
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<string>(),
                    TestProjectPath,
                    outputPath: null,
                    rootNamespace: "E2E.Sqlite",
                    contextName: null,
                    useDataAnnotations: UseDataAnnotations,
                    overwriteFiles: false,
                    useDatabaseNames: false);

                Assert.Contains("warn: " + DesignStrings.MissingPrimaryKey("Principal"), Reporter.Messages);
                Assert.Contains("warn: " + DesignStrings.UnableToGenerateEntityType("Principal"), Reporter.Messages);
                Assert.Contains("warn: " + DesignStrings.ForeignKeyScaffoldErrorPrincipalTableScaffoldingError("DependentNoPrincipalPk(PrincipalId)", "Principal"), Reporter.Messages);

                var expectedFileSet = new FileSet(new FileSystemFileService(), Path.Combine(ExpectedResultsParentDir, "NoPrincipalPk"))
                {
                    Files =
                    {
                        "NoPrincipalPk" + DbSuffix + "Context.cs",
                        "DependentNoPrincipalPk.cs"
                    }
                };
                var actualFileSet = new FileSet(InMemoryFiles, TestProjectFullPath)
                {
                    Files = Enumerable.Repeat(results.ContextFile, 1).Concat(results.EntityTypeFiles).Select(Path.GetFileName).ToList()
                };
                AssertEqualFileContents(expectedFileSet, actualFileSet);
                AssertCompile(actualFileSet);
            }
        }

        [Fact]
        public virtual void It_handles_unsafe_names()
        {
            using (var testStore = SqliteTestStore.GetOrCreateInitialized("UnsafeNames" + DbSuffix))
            {
                testStore.ExecuteNonQuery(@"
CREATE TABLE 'Named with space' ( Id PRIMARY KEY );
CREATE TABLE '123 Invalid Class Name' ( Id PRIMARY KEY);
CREATE TABLE 'Bad characters `~!@#$%^&*()+=-[];''"",.<>/?|\ ' ( Id PRIMARY KEY);
CREATE TABLE ' Bad columns ' (
    'Space jam' PRIMARY KEY,
    '123 Go`',
    'Bad to the bone. `~!@#$%^&*()+=-[];''"",.<>/?|\ ',
    'Next one is all bad',
    '@#$%^&*()'
);
CREATE TABLE Keywords (
    namespace PRIMARY KEY,
    virtual,
    public,
    class,
    string,
    FOREIGN KEY (class) REFERENCES string (string)
);
CREATE TABLE String (
    string PRIMARY KEY,
    FOREIGN KEY (string) REFERENCES String (string)
);
");

                var results = Generator.Generate(
                    testStore.ConnectionString,
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<string>(),
                    TestProjectPath,
                    outputPath: null,
                    rootNamespace: "E2E.Sqlite",
                    contextName: null,
                    useDataAnnotations: UseDataAnnotations,
                    overwriteFiles: false,
                    useDatabaseNames: false);

                Assert.Empty(Reporter.Messages.Where(m => m.StartsWith("warn: ")));

                var files = new FileSet(InMemoryFiles, TestProjectFullPath)
                {
                    Files = Enumerable.Repeat(results.ContextFile, 1).Concat(results.EntityTypeFiles).Select(Path.GetFileName).ToList()
                };
                AssertCompile(files);
            }
        }

        [Fact]
        public virtual void Foreign_key_to_unique_index()
        {
            using (var testStore = SqliteTestStore.GetOrCreateInitialized("FkToAltKey" + DbSuffix))
            {
                testStore.ExecuteNonQuery(@"
CREATE TABLE User (
    Id INTEGER PRIMARY KEY,
    AltId INTEGER NOT NULL UNIQUE
);
CREATE TABLE Comment (
    Id INTEGER PRIMARY KEY,
    UserAltId INTEGER NOT NULL,
    Contents TEXT,
    FOREIGN KEY (UserAltId) REFERENCES User (AltId)
);");

                var results = Generator.Generate(
                    testStore.ConnectionString,
                    new[] { "User", "Comment" },
                    Enumerable.Empty<string>(),
                    TestProjectPath,
                    outputPath: null,
                    rootNamespace: "E2E.Sqlite",
                    contextName: "FkToAltKeyContext",
                    useDataAnnotations: UseDataAnnotations,
                    overwriteFiles: false,
                    useDatabaseNames: false);

                Assert.Empty(Reporter.Messages.Where(m => m.StartsWith("warn: ")));

                var expectedFileSet = new FileSet(new FileSystemFileService(), Path.Combine(ExpectedResultsParentDir, "FkToAltKey"))
                {
                    Files =
                    {
                        "FkToAltKeyContext.cs",
                        "Comment.cs",
                        "User.cs"
                    }
                };
                var actualFileSet = new FileSet(InMemoryFiles, TestProjectFullPath)
                {
                    Files = Enumerable.Repeat(results.ContextFile, 1).Concat(results.EntityTypeFiles).Select(Path.GetFileName).ToList()
                };
                AssertEqualFileContents(expectedFileSet, actualFileSet);
                AssertCompile(actualFileSet);
            }
        }

        protected override ICollection<BuildReference> References { get; } = new List<BuildReference>
        {
            BuildReference.ByName("Microsoft.EntityFrameworkCore"),
            BuildReference.ByName("Microsoft.EntityFrameworkCore.Relational"),
            BuildReference.ByName("Microsoft.EntityFrameworkCore.Sqlite")
        };

        protected abstract string DbSuffix { get; } // will be used to create different databases so tests running in parallel don't interfere
        protected abstract string ExpectedResultsParentDir { get; }
        protected abstract bool UseDataAnnotations { get; }
    }
}
