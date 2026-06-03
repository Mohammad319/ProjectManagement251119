using Domain.Entities.Base;
using Domain.Entities.Calculation;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public sealed class CompensationEntity : ColoredListEntity
    {
        public bool IsDefault { get; private set; }

        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; private set; } = [];

        [JsonIgnore]
        public ICollection<ProjectEntity> Projects { get; private set; } = [];

        public CompensationEntity() { }

        public CompensationEntity(string name, string color, int sortOrder, bool isVisible = true)
            : base(name, color, sortOrder, isVisible) { }

        public void SetIsDefault(bool isDefault) => IsDefault = isDefault;
    }
}
