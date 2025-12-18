using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.General;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class StatusResourcesEntity : AuditableEntity<int>, IListOrderDTO
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;
        [Required, StringLength(FieldLengths.ColorHex, MinimumLength = FieldLengths.ColorHex)]
        public string Color { get; private set; } = "#00ff00";
        public int SortOrder { get; private set; }
        public bool IsVisible { get; private set; } = true;
        [JsonIgnore] public ICollection<ResourceEntity> Resources { get; set; } = [];
        public StatusResourcesEntity() { }

        // Domain constructor
        public StatusResourcesEntity(string name, string color, int sortOrder, bool isVisible = true)
        {
            Update(name, color, sortOrder, isVisible);
        }
        public void Update(string name, string color, int sortOrder, bool isVisible)
        {
            Name = name;
            Color = color;
            SortOrder = sortOrder;
            IsVisible = isVisible;
        }
    }
}
