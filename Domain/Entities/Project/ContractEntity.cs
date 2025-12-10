using Domain.Entities.Base;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public sealed class ContractEntity : AuditableEntity<int>
    {
        public int SortOrder { get; set; }

        public bool IsVisible { get; set; } = true;

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(FieldLengths.ColorHex, MinimumLength = FieldLengths.ColorHex)]
        public string Color { get; set; } = "#00ff00";

        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; set; } = [];

        [JsonIgnore]
        public ICollection<ProjectEntity> Projects { get; set; } = [];
    }

}
