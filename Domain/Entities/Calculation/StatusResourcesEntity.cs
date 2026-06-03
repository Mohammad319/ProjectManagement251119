using Domain.Entities.Base;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class StatusResourcesEntity : ColoredListEntity, IControlStatusEntity
    {
        [JsonIgnore]
        public ICollection<ResourceEntity> Resources { get; private set; } = [];

        public string Code { get; private set; } = string.Empty;
        public bool IsDefault { get; private set; }
        public bool IsSystemDefault { get; private set; }

        public StatusResourcesEntity() { }

        public StatusResourcesEntity(string name, string color, int sortOrder, bool isVisible = true)
            : base(name, color, sortOrder, isVisible) { }

        public void SetControlStatusSettings(string? code, bool isDefault, bool isSystemDefault)
        {
            Code = NormalizeCode(code);
            IsDefault = isDefault;
            IsSystemDefault = isSystemDefault;
        }

        private static string NormalizeCode(string? code)
        {
            var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
            return normalized.Length > 64 ? normalized[..64] : normalized;
        }
    }
}
