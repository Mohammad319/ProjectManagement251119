 using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Project;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Users
{
    /// <summary>
    /// Represents an application user that belongs to a specific tenant.
    /// </summary>
    public sealed class UserEntity : AuditableEntity<int>
    {
        /// <summary>External authentication provider user id (Azure AD / Identity).</summary>
        [MaxLength(200)]
        public string ExternalAuthId { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(FieldLengths.Email)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Optional department the user belongs to.
        /// </summary>
        public int? DepartmentId { get; set; }
        [MaxLength(80)]
        public string? FirstName { get; set; }
        [MaxLength(80)]
        public string? LastName { get; set; }

        /// <summary>
        /// Navigation to the user's department.
        /// </summary>
        public DepartmentEntity? Department { get; set; }

    }
}
