using Domain.Entities.Base;
using Domain.Entities.Project;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Project;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Folder
{
    public class FolderEntity : IDataKeyFilterReadOnly
    {
        public Guid Id { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(50, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string Name { get; set; }

        [StringLength(7, ErrorMessageResourceName = ErrorsMessages.StringLength, ErrorMessageResourceType = typeof(ResLocalize), MinimumLength = 7)]
        public string Color { get; set; } = "#08bf66";
        public double Order { get; set; }
        public bool IsVisible { get; set; } = true;
        [JsonIgnore] public int TenantId { get; set; }

        /// <summary>
        /// Department that owns this folder (required).
        /// </summary>
        public int DepartmentId { get; set; }

        [JsonIgnore]
        public DepartmentEntity Department { get; set; } = null!;

        /// <summary>
        /// Optional user that owns this folder.
        /// </summary>
        public int? UserId { get; set; }

        [JsonIgnore]
        public UserEntity? User { get; set; }

        /// <summary>
        /// Projects contained in this folder.
        /// </summary>
        public ICollection<ProjectEntity> Projects { get; set; } = [];
    }
}
