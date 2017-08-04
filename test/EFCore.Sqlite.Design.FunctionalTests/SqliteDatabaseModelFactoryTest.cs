// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore
{
    public class SqliteDatabaseModelFactoryTest : IClassFixture<SqliteDatabaseModelFactoryTest.SqliteDatabaseModelFixture>
    {
        protected SqliteDatabaseModelFixture Fixture { get; }

        public SqliteDatabaseModelFactoryTest(SqliteDatabaseModelFixture fixture) => Fixture = fixture;

        [Fact]
        public void It_reads_tables()
        {
            Fixture.TestStore.Clean(Fixture.CreateContext());

            var sql = @"CREATE TABLE [Everest] ( id int );  CREATE TABLE [Denali] ( id int );";

            var dbModel = CreateModel(sql);

            Assert.Collection(
                dbModel.Tables.OrderBy(t => t.Name),
                d => Assert.Equal("Denali", d.Name),
                e => Assert.Equal("Everest", e.Name));
        }

        [Fact]
        public void It_reads_foreign_keys()
        {
            var sql = "CREATE TABLE Ranges ( Id INT IDENTITY (1,1) PRIMARY KEY);" +
                      "CREATE TABLE Mountains ( RangeId INT NOT NULL, FOREIGN KEY (RangeId) REFERENCES Ranges(Id) ON DELETE CASCADE)";

            var dbModel = CreateModel(sql);

            var fk = Assert.Single(dbModel.Tables.Single(t => t.Name == "Mountains").ForeignKeys);

            Assert.Equal("Mountains", fk.Table.Name);
            Assert.Equal("Ranges", fk.PrincipalTable.Name);
            Assert.Equal("RangeId", fk.Columns.Single().Name);
            Assert.Equal("Id", fk.PrincipalColumns.Single().Name);
            Assert.Equal(ReferentialAction.Cascade, fk.OnDelete);
        }

        [Fact]
        public void It_reads_composite_foreign_keys()
        {
            var sql = "CREATE TABLE CompositeRanges ( Id INT IDENTITY (1,1), AltId INT, PRIMARY KEY(Id, AltId));" +
                      "CREATE TABLE CompositeMountains ( RangeId INT NOT NULL, RangeAltId INT NOT NULL, FOREIGN KEY (RangeId, RangeAltId) " +
                      " REFERENCES CompositeRanges(Id, AltId) ON DELETE NO ACTION)";
            var dbModel = CreateModel(sql);

            var fk = Assert.Single(dbModel.Tables.Single(t => t.ForeignKeys.Count > 0).ForeignKeys);

            Assert.NotNull(fk);
            Assert.Equal("CompositeMountains", fk.Table.Name);
            Assert.Equal("CompositeRanges", fk.PrincipalTable.Name);
            Assert.Equal(new[] { "RangeId", "RangeAltId" }, fk.Columns.Select(c => c.Name).ToArray());
            Assert.Equal(new[] { "Id", "AltId" }, fk.PrincipalColumns.Select(c => c.Name).ToArray());
            Assert.Equal(ReferentialAction.NoAction, fk.OnDelete);
        }

        [Fact]
        public void It_reads_indexes_unique_constraints_and_primary_keys()
        {
            var sql = "CREATE TABLE Place ( Id int PRIMARY KEY, Name int UNIQUE, Location int);" +
                      "CREATE INDEX IX_Location_Name ON Place (Location, Name);";

            var dbModel = CreateModel(sql);

            var table = dbModel.Tables.Single(t => t.Name == "Place");
            var pkIndex = table.PrimaryKey;

            Assert.Equal("Place", pkIndex.Table.Name);
            Assert.Equal(new List<string> { "Id" }, pkIndex.Columns.Select(ic => ic.Name).ToList());

            var constraints = table.UniqueConstraints;

            Assert.All(constraints, c => { Assert.Equal("Place", c.Table.Name); });

            Assert.Collection(
                constraints,
                unique => { Assert.Equal("Name", unique.Columns.Single().Name); });

            var indexes = table.Indexes;

            Assert.All(indexes, c => { Assert.Equal("Place", c.Table.Name); });

            Assert.Collection(
                indexes,
                index =>
                    {
                        Assert.Equal("IX_Location_Name", index.Name);
                        Assert.False(index.IsUnique);
                        Assert.Equal(new List<string> { "Location", "Name" }, index.Columns.Select(ic => ic.Name).ToList());
                    });
        }

        [Fact]
        public void It_reads_columns()
        {
            var sql = @"
CREATE TABLE [MountainsColumns] (
    Id integer primary key,
    Name string NOT NULL,
    Latitude numeric DEFAULT 0.0,
    Created datetime DEFAULT('October 20, 2015 11am')
);";
            var dbModel = CreateModel(sql);

            var columns = dbModel.Tables.Single().Columns;

            Assert.All(columns, c => { Assert.Equal("MountainsColumns", c.Table.Name); });

            Assert.Collection(
                columns,
                id =>
                    {
                        Assert.Equal("Id", id.Name);
                        Assert.Equal("integer", id.StoreType);
                        Assert.False(id.IsNullable);
                        Assert.Null(id.DefaultValueSql);
                    },
                name =>
                    {
                        Assert.Equal("Name", name.Name);
                        Assert.Equal("string", name.StoreType);
                        Assert.False(name.IsNullable);
                        Assert.Null(name.DefaultValueSql);
                    },
                lat =>
                    {
                        Assert.Equal("Latitude", lat.Name);
                        Assert.Equal("numeric", lat.StoreType);
                        Assert.True(lat.IsNullable);
                        Assert.Equal("0.0", lat.DefaultValueSql);
                    },
                created =>
                    {
                        Assert.Equal("Created", created.Name);
                        Assert.Equal("datetime", created.StoreType);
                        Assert.Equal("'October 20, 2015 11am'", created.DefaultValueSql);
                    });
        }

        [Fact]
        public void It_filters_tables()
        {
            var sql = @"CREATE TABLE [K2] ( Id int);
CREATE TABLE [Kilimanjaro] ( Id int);";

            var selectionSet = new List<string> { "K2" };

            var dbModel = CreateModel(sql, selectionSet);
            var table = Assert.Single(dbModel.Tables);
            Assert.NotNull(table);
            Assert.Equal("K2", table.Name);
        }

        public DatabaseModel CreateModel(string createSql, IEnumerable<string> tables = null)
        {
            Fixture.TestStore.ExecuteNonQuery(createSql);

            return Fixture.Factory.Create(Fixture.TestStore.ConnectionString, tables ?? Enumerable.Empty<string>(), Enumerable.Empty<string>());
        }

        public class SqliteDatabaseModelFixture : SharedStoreFixtureBase<DbContext>
        {
            protected override string StoreName { get; } = "DatabaseModelTest";
            protected override ITestStoreFactory<TestStore> TestStoreFactory => SqliteTestStoreFactory.Instance;
            public new SqliteTestStore TestStore => (SqliteTestStore)base.TestStore;
            public TestDesignLoggerFactory TestDesignLoggerFactory { get; } = new TestDesignLoggerFactory();
            public SqliteDatabaseModelFactory Factory { get; }

            public SqliteDatabaseModelFixture()
            {
                var reporter = new TestOperationReporter();
                var serviceCollection = new ServiceCollection().AddScaffolding(reporter)
                    .AddSingleton<IOperationReporter>(reporter);
                new SqliteDesignTimeServices().ConfigureDesignTimeServices(serviceCollection);

                var serviceProvider = serviceCollection.BuildServiceProvider();

                Factory = serviceProvider
                    .GetService<IDatabaseModelFactory>() as SqliteDatabaseModelFactory;
            }
        }
    }
}
