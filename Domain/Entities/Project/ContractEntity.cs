using Domain.Entities.Base;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.General;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public sealed class ContractEntity : AuditableEntity<int>, IListOrderDTO
    {
        public int SortOrder { get; private set; }

        public bool IsVisible { get; private set; } = true;

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        [Required, StringLength(FieldLengths.ColorHex, MinimumLength = FieldLengths.ColorHex)]
        public string Color { get; private set; } = "#00ff00";

        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; set; } = [];

        [JsonIgnore]
        public ICollection<ProjectEntity> Projects { get; set; } = [];
        private ContractEntity() { }

        public ContractEntity(string name, string color, int sortOrder, bool isVisible = true)
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
