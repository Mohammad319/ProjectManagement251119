using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AuthPermissions.Context
{
    public class ApplicationUser : IdentityUser
    {

        [MaxLength(50)]
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiryTime { get; set; }
        public int? DepartmentId { get; set; }
        public int? TenantId { get; set; }
        [MaxLength(128)]
        public string DB { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public DateTimeOffset? LockoutStart { get; set; }

        [MaxLength(100)]
        public string Firstname { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Lastname { get; set; } = string.Empty;
    }
}
