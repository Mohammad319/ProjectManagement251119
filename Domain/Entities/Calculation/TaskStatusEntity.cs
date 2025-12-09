using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class TaskStatusEntity : AuditableEntity<int>
    {
        public int SortOrder { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string? Name { get; set; }
        [StringLength(7, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize), MinimumLength = 7)]
        public string Color { get; set; } = "#00ff00";
        public bool IsVisible { get; set; } = true;
        [JsonIgnore] public ICollection<TaskEntity> Tasks { get; set; } = [];
    }
}
