using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Project;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Users
{
    /// <summary>
    /// Represents an application user that belongs to a specific tenant.
    /// </summary>
    public sealed class UserEntity : AuditableEntity<int>
    {
        /// <summary>External authentication provider user id (Azure AD / Identity).</summary>
        [Required]
        public string ExternalAuthId { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Optional department the user belongs to.
        /// </summary>
        public int? DepartmentId { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        /// <summary>
        /// Navigation to the user's department.
        /// </summary>
        public DepartmentEntity? Department { get; set; }

        /// <summary>
        /// Folders owned by the user.
        /// </summary>
        public ICollection<FolderEntity> Folders { get; set; } = [];

        /// <summary>
        /// Projects created/owned by the user.
        /// </summary>
        public ICollection<ProjectEntity> Projects { get; set; } = [];

        /// <summary>
        /// Calculations created by the user.
        /// </summary>
        public ICollection<CalculationEntity> Calculations { get; set; } = [];
    }
}
