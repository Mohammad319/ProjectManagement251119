using Domain.Entities.Base;
using Domain.Entities.Calculation;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public sealed class StatusEntity : ColoredListEntity
    {
        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; private set; } = [];

        public StatusEntity() { }

        public StatusEntity(string name, string color, int sortOrder, bool isVisible = true)
            : base(name, color, sortOrder, isVisible) { }
    }
}
