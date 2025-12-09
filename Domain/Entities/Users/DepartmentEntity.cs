using Domain.Entities.Base;
using Domain.Entities.Folder;
using Domain.Entities.Project;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Users
{
    /// <summary>
    /// Logical department within a tenant.
    /// </summary>
    public sealed class DepartmentEntity : AuditableEntity<int>
    {
        // Id من IntBaseEntity
        // TenantId من IntBaseEntity

        [Required, MaxLength(FieldLengths.Name)]
        public required string Name { get; set; }

        [MaxLength(
            500,
            ErrorMessageResourceName = ErrorsMessages.MaxLength,
            ErrorMessageResourceType = typeof(ResLocalize))]
        public string? Description { get; set; }

        /// <summary>
        /// Creation time of this department.
        /// </summary>
        public DateTime Created { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last modification time.
        /// </summary>
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Folders belonging to this department.
        /// </summary>
        [JsonIgnore]
        public ICollection<FolderEntity> Folders { get; set; } = new List<FolderEntity>();

        /// <summary>
        /// Users assigned to this department.
        /// </summary>
        [JsonIgnore]
        public ICollection<UserEntity> Users { get; set; } = new List<UserEntity>();

        /// <summary>
        /// Projects owned by this department.
        /// </summary>
        [JsonIgnore]
        public ICollection<ProjectEntity> Projects { get; set; } = [];
    }

}
