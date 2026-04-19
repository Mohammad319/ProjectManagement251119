using Domain.Entities.Base;
using Domain.Entities.Calculation;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public sealed class ProcurementMethodEntity : ColoredListEntity
    {
        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; private set; } = [];

        [JsonIgnore]
        public ICollection<ProjectEntity> Projects { get; private set; } = [];

        public ProcurementMethodEntity() { }

        public ProcurementMethodEntity(string name, string color, int sortOrder, bool isVisible = true)
            : base(name, color, sortOrder, isVisible) { }
    }
}
