using ProjectManagement.Shared.Constant;
using System;

namespace ProjectManagement.Shared.Models.Account
{
    public class UserAuthModel
    {
        public string Id { get; set; }
        public string FullName => $"{Firstname} {Lastname}";

        public string Email { get; set; }
        public string Username { get; set; }
        public int? DepartmentId { get; set; }
        public int? TenantId { get; set; }
        public string NormalizedEmail { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public int? UserId { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutStart { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
    }
    public class UserPostDTO
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public DateTimeOffset? LockoutStart { get; set; }
        public int? DepartmentId { get; set; }
        public string Role { get; set; } = PMRolesConst.Tenant.Manger;

    }
}
