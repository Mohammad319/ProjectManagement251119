using Domain.Entities.Base;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class StatusResourcesEntity : ColoredListEntity
    {
        [JsonIgnore]
        public ICollection<ResourceEntity> Resources { get; private set; } = [];

        public StatusResourcesEntity() { }

        public StatusResourcesEntity(string name, string color, int sortOrder, bool isVisible = true)
            : base(name, color, sortOrder, isVisible) { }
    }
}
