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
        [Required, MaxLength(FieldLengths.Name)]
        public required string Name { get; set; }

        [MaxLength(FieldLengths.Comment)]
        public string? Description { get; set; }

        /// <summary>
        /// Folders belonging to this department.
        /// </summary>
        [JsonIgnore]
        public ICollection<FolderEntity> Folders { get; set; } = [];

        /// <summary>
        /// Users assigned to this department.
        /// </summary>
        [JsonIgnore]
        public ICollection<UserEntity> Users { get; set; } = [];

        /// <summary>
        /// Projects owned by this department.
        /// </summary>
        [JsonIgnore]
        public ICollection<ProjectEntity> Projects { get; set; } = [];
    }

}
