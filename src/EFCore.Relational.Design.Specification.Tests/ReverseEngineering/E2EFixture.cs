// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.ReverseEngineering
{
    public abstract class E2EFixture
    {
        public TestOperationReporter Reporter { get; }
        public InMemoryFileService InMemoryFiles { get; private set; }
        public IModelScaffolder Generator { get; }
        public IScaffoldingModelFactory ScaffoldingModelFactory { get; }

        protected E2EFixture()
        {
            Reporter = new TestOperationReporter();

            var serviceBuilder = new ServiceCollection()
                .AddSingleton<IOperationReporter>(Reporter)
                .AddScaffolding(Reporter)
                .AddLogging();

            ConfigureDesignTimeServices(serviceBuilder);

            var serviceProvider = serviceBuilder
                .AddSingleton(typeof(IFileService), sp => InMemoryFiles = new InMemoryFileService())
                .BuildServiceProvider(validateScopes: true);

            Generator = serviceProvider.GetRequiredService<IModelScaffolder>();
            ScaffoldingModelFactory = serviceProvider.GetRequiredService<IScaffoldingModelFactory>();
        }

        protected abstract void ConfigureDesignTimeServices(IServiceCollection services);
    }
}
