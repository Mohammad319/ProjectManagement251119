using Domain.Entities.Base;
using Domain.Entities.Project;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Project;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain.Entities.Folder
{
    public class FolderEntity : FolderBase, IDataKeyFilterReadOnly
    {
        public Guid Id { get; set; }
        [JsonIgnore] public int TenantId { get; set; }

        public int DepartmentId { get; set; }
        [JsonIgnore] public DepartmentEntity Department { get; set; }
        public int? UserId { get; set; }
        [JsonIgnore] public UserEntity User { get; set; }

        public ICollection<ProjectEntity> Projects { get; set; }
        //public Guid? FolderId { get; set; }
        //public FolderEntity Folder { get; set; }
        //public ICollection<FolderEntity> Folders { get; set; }
    }
}
