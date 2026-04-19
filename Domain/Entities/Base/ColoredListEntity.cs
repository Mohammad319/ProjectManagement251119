using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.General;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Base
{
    public abstract class ColoredListEntity : AuditableEntity<int>, IListOrderDTO
    {
        private const string DefaultColor = "#00ff00";

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; protected set; } = string.Empty;

        [Required, StringLength(FieldLengths.ColorHex, MinimumLength = FieldLengths.ColorHex)]
        public string Color { get; protected set; } = DefaultColor;

        public int SortOrder { get; protected set; }
        public bool IsVisible { get; protected set; } = true;

        protected ColoredListEntity() { }

        protected ColoredListEntity(string name, string color, int sortOrder, bool isVisible = true)
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
                throw new ValidationException("Name is required.");
            if (trimmed.Length > FieldLengths.Name)
                throw new ValidationException($"Name exceeds max length {FieldLengths.Name}.");
            return trimmed;
        }

        private static string NormalizeColor(string? value)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new ValidationException("Color is required.");
            if (trimmed.Length != FieldLengths.ColorHex || trimmed[0] != '#' || !trimmed[1..].All(Uri.IsHexDigit))
                throw new ValidationException("Color must be a valid 6-digit HEX value like #00ff00.");
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
