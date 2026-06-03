using Domain.Entities.Base;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public sealed class ProcurementProcedureEntity : ColoredListEntity
    {
        public bool IsDefault { get; private set; }

        [JsonIgnore]
        public ICollection<ProjectEntity> Projects { get; private set; } = [];

        public ProcurementProcedureEntity() { }

        public ProcurementProcedureEntity(string name, string color, int sortOrder, bool isVisible = true)
            : base(name, color, sortOrder, isVisible) { }

        public void SetIsDefault(bool isDefault) => IsDefault = isDefault;
    }
}
