using Domain.Entities.Base;
using Domain.Entities.Users;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class TaskStatusEntity : AuditableEntity<int>
    {
        public int SortOrder { get; set; }
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; set; } = string.Empty;
        [Required, StringLength(FieldLengths.ColorHex, MinimumLength = FieldLengths.ColorHex)]
        public string Color { get; set; } = "#00ff00";
        public bool IsVisible { get; set; } = true;
        [ForeignKey(nameof(CreatedBy))]
        [JsonIgnore]
        public UserEntity? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        [JsonIgnore]
        public UserEntity? UpdatedByUser { get; set; }

        [JsonIgnore] public ICollection<TaskEntity> Tasks { get; set; } = [];
    }
}
