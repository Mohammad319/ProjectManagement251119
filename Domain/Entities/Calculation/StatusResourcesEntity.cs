using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class StatusResourcesEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; set; } = string.Empty;
        [Required, StringLength(FieldLengths.ColorHex, MinimumLength = FieldLengths.ColorHex)]
        public string Color { get; set; } = "#00ff00";
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;
        [JsonIgnore] public ICollection<ResourceEntity> Resources { get; set; } = [];
    }
}
