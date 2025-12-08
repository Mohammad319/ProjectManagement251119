using Domain.Entities.Base;
using Domain.Entities.Folder;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class TaskStatusEntity : IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        public int Order { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string Name { get; set; }
        [StringLength(7, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize), MinimumLength = 7)]
        public string Color { get; set; } = "#00ff00";
        [JsonIgnore] public int TenantId { get; set; }
        public bool IsVisible { get; set; } = true;
        [JsonIgnore] public ICollection<TaskEntity> Tasks { get; set; }
    }
}
