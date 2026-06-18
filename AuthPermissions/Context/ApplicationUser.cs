using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AuthPermissions.Context
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(256)]
        public string RefreshToken { get; set; } = string.Empty;

        public DateTime RefreshTokenExpiryTime { get; set; }

        public int? DepartmentId { get; set; }

        public int? TenantId { get; set; }

        [MaxLength(128)]
        public string DB { get; set; } = string.Empty;

        public int? UserId { get; set; }

        public DateTimeOffset? LockoutStart { get; set; }

        /// <summary>Timestamp of the user's most recent successful sign-in.</summary>
        public DateTimeOffset? LastLoginAt { get; set; }

        /// <summary>Functional account state. When false the account is deactivated and cannot sign in,
        /// independently of the (temporary, security-driven) lockout mechanism.</summary>
        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string Firstname { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Lastname { get; set; } = string.Empty;
    }
}
