using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Project
{
    /// <summary>
    /// Admin setting for one dropdown category/tab (e.g. "ProjectType", "CalculationStatus"):
    /// whether a value must be selected before a project/calculation can be saved.
    /// One row per (TenantId, Category); categories without a row fall back to
    /// <see cref="DropdownCategoryConst.DefaultIsRequired"/>. The setting belongs to the
    /// category itself — never to individual rows/values in the list.
    /// TenantId is assigned automatically by the audit/tenant interceptor.
    /// </summary>
    public sealed class DropdownSettingEntity : AuditableEntity<int>
    {
        [Required, MaxLength(60)]
        public string Category { get; private set; } = string.Empty;

        public bool IsRequired { get; private set; }

        private DropdownSettingEntity() { }

        public static DropdownSettingEntity Create(string category, bool isRequired)
        {
            return new DropdownSettingEntity
            {
                Category = NormalizeCategory(category),
                IsRequired = isRequired
            };
        }

        public void SetIsRequired(bool isRequired) => IsRequired = isRequired;

        private static string NormalizeCategory(string? category)
        {
            var normalized = category?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ValidationException("Category is required.");

            return normalized.Length > 60 ? normalized[..60] : normalized;
        }
    }
}
