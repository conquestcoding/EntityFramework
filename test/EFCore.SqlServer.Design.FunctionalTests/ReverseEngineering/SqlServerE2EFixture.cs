// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.ReverseEngineering
{
    public class SqlServerE2EFixture : E2EFixture
    {
        protected override void ConfigureDesignTimeServices(IServiceCollection services)
            => new SqlServerDesignTimeServices().ConfigureDesignTimeServices(services);
    }
}
