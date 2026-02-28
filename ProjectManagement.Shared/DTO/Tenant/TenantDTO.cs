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
    }
}
