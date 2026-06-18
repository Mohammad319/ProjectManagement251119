using System;

namespace Domain.DTO.User
{
    public class TenantUserDto
    {
        public int Id { get; set; }
        public string? IdAuth { get; set; }
        public string? Role { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public int? DepartmentId { get; set; }
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public bool IsInAuth { get; set; }
        public DateTimeOffset? LockoutStart { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }

        /// <summary>Functional account state (false = deactivated). Defaults to true for users not yet in auth.</summary>
        public bool IsActive { get; set; } = true;
    }
}
