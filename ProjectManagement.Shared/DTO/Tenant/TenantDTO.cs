using ProjectManagement.Shared.DTO.Identity;
using ProjectManagement.Shared.Base.Users;
using System;
using System.Collections.Generic;

namespace ProjectManagement.Shared.DTO.Tenant
{

    public class GetTenantsDTO
    {
        public int Id { get; set; }
        public DateTimeOffset? DateExpire { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DB { get; set; } = string.Empty;
        public bool HasOwnDb { get; set; }
        public string DatabaseInfoName { get; set; } = string.Empty;

        /// <summary>Number of users linked to this tenant.</summary>
        public int UsersCount { get; set; }

        /// <summary>True when the tenant has users and all of them are locked out (blocked).</summary>
        public bool IsBlocked { get; set; }
    }

    /// <summary>Outcome of seeding a tenant's default reference data (statuses, accounts, departments, lookups…).</summary>
    public class CompanySeedResultDTO
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public bool AnyChanges => Created > 0 || Updated > 0;
    }
}
