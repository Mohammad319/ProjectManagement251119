using Domain.Entities.Base;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.General;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public sealed class TypeEntity : AuditableEntity<int>, IListOrderDTO
    {
        private const string DefaultColor = "#00ff00";

        public int SortOrder { get; private set; }
        public bool IsVisible { get; private set; } = true;

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        [Required, StringLength(FieldLengths.ColorHex, MinimumLength = FieldLengths.ColorHex)]
        public string Color { get; private set; } = DefaultColor;

        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; private set; } = [];

        [JsonIgnore]
        public ICollection<ProjectEntity> Projects { get; private set; } = [];

        public TypeEntity() { }

        public TypeEntity(string name, string color, int sortOrder, bool isVisible = true)
        {
            Update(name, color, sortOrder, isVisible);
        }

        public void Update(string name, string color, int sortOrder, bool isVisible)
        {
            Name = NormalizeName(name);
            Color = NormalizeColor(color);
            SortOrder = NormalizeSortOrder(sortOrder);
            IsVisible = isVisible;
        }

        private static string NormalizeName(string? value)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new ValidationException("Project type name is required.");
            return trimmed;
        }

        private static string NormalizeColor(string? value)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new ValidationException("Project type color is required.");
            if (trimmed.Length != FieldLengths.ColorHex || trimmed[0] != '#' || !trimmed[1..].All(Uri.IsHexDigit))
                throw new ValidationException("Project type color must be a valid HEX value like #00ff00.");
            return trimmed.ToLowerInvariant();
        }

        private static int NormalizeSortOrder(int sortOrder)
        {
            if (sortOrder < 0)
                throw new ValidationException("SortOrder cannot be negative.");
            return sortOrder;
        }
    }
}
