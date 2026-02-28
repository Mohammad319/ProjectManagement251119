using ProjectManagement.Shared.Constant;
using System;

namespace ProjectManagement.Shared.Models.Account
{
    public class UserAuthModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName => $"{Firstname} {Lastname}";

        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string NormalizedEmail { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool PhoneNumberConfirmed { get; set; }
        public int? UserId { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutStart { get; set; }
        public string Firstname { get; set; } = string.Empty;
        public string Lastname { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

    }
    public class UserPostDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool PhoneNumberConfirmed { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public string Firstname { get; set; } = string.Empty;
        public string Lastname { get; set; } = string.Empty;
        public DateTimeOffset? LockoutStart { get; set; }
        public int? DepartmentId { get; set; }
        public string Role { get; set; } = PMRolesConst.Tenant.Manger;

    }
}
