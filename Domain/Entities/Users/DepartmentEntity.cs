using Domain.Entities.Base;
using Domain.Entities.Folder;
using Domain.Entities.Project;
using ProjectManagement.Shared.Base.Users;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Users
{
    public class DepartmentEntity : DepartmentBase, IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(60, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string Name { get; set; }
        [MaxLength(500, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string Description { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? LastModified { get; set; }

        [JsonIgnore] public ICollection<FolderEntity> Folders { get; set; }
        [JsonIgnore] public ICollection<UserEntity> Users { get; set; }
        [JsonIgnore] public ICollection<ProjectEntity> Projects { get; set; }
    }
}
