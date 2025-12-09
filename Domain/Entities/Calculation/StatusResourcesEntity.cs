using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class StatusResourcesEntity : AuditableEntity<int>
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string Name { get; set; } = string.Empty;
        [StringLength(7, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize), MinimumLength = 7)]
        public string Color { get; set; } = "#00ff00";
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;
        [JsonIgnore] public ICollection<ResourceEntity> Resources { get; set; } = [];
    }
}
