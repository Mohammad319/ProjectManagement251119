using Domain.Entities.Base;
using Domain.Entities.Folder;
using Domain.Entities.Project;
using ProjectManagement.Shared.Base.Users;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain.Entities.Users
{
    public class DepartmentEntity : DepartmentBase, IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? LastModified { get; set; }

        [JsonIgnore] public ICollection<FolderEntity> Folders { get; set; }
        [JsonIgnore] public ICollection<UserEntity> Users { get; set; }
        [JsonIgnore] public ICollection<ProjectEntity> Projects { get; set; }
    }
}
