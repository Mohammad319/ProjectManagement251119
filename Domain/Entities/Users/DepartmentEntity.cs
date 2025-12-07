using Domain.Entities.Base;
using Domain.Entities.Folder;
using Domain.Entities.Project;
using ProjectManagement.Shared.Base.Users;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain.Entities.Users
{
    /// <summary>
    /// Logical department within a tenant.
    /// </summary>
    public sealed class DepartmentEntity : DepartmentBase, IDataKeyFilterReadOnly
    {
        public int Id { get; set; }

        [JsonIgnore]
        public int TenantId { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? LastModified { get; set; }

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
