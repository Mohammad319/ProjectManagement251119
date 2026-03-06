using Domain.Entities.Base;
using Domain.Entities.Project;
using Domain.Entities.Users;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Domain.Entities.Folder
{
    public sealed class FolderEntity : AuditableEntity<Guid>
    {
        private static readonly Regex HexColorRegex = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        [Required, StringLength(FieldLengths.ColorHex, MinimumLength = FieldLengths.ColorHex)]
        public string Color { get; private set; } = "#08BF66";

        public double SortOrder { get; private set; }

        public bool IsVisible { get; private set; } = true;

        public int DepartmentId { get; private set; }

        [JsonIgnore]
        public DepartmentEntity Department { get; private set; } = null!;

        [JsonIgnore]
        public ICollection<ProjectEntity> FolderProjects { get; private set; } = [];

        private FolderEntity() { }

        public FolderEntity(string name, string color, int departmentId, int createdBy, double sortOrder)
        {
            if (departmentId <= 0)
                throw new ArgumentOutOfRangeException(nameof(departmentId));

            if (createdBy <= 0)
                throw new ArgumentOutOfRangeException(nameof(createdBy));

            DepartmentId = departmentId;
            CreatedBy = createdBy;

            Update(name, color, true);
            UpdateOrder(sortOrder);
        }

        public void Update(string name, string color, bool isVisible)
        {
            Name = NormalizeName(name);
            Color = NormalizeColor(color);
            IsVisible = isVisible;
        }

        public void UpdateOrder(double newOrder)
        {
            if (double.IsNaN(newOrder) || double.IsInfinity(newOrder))
                throw new ArgumentOutOfRangeException(nameof(newOrder), "SortOrder must be a finite number.");

            SortOrder = newOrder;
        }

        private static string NormalizeName(string? name)
        {
            var normalized = (name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalized))
                throw new ValidationException("Folder name is required.");

            if (normalized.Length > FieldLengths.Name)
                throw new ValidationException($"Folder name exceeds max length {FieldLengths.Name}.");

            return normalized;
        }

        private static string NormalizeColor(string? color)
        {
            var normalized = (color ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(normalized))
                normalized = "#08BF66";

            if (!normalized.StartsWith('#'))
                normalized = $"#{normalized}";

            if (!HexColorRegex.IsMatch(normalized))
                throw new ValidationException("Folder color must be a valid 6-digit hex color like #08BF66.");

            return normalized;
        }
    }
}
