using System;
using System.Collections.Generic;

namespace E2E.Sqlite
{
    public partial class DependentNoPrincipalPk
    {
        public string Id { get; set; }
        public long? PrincipalId { get; set; }
    }
}
