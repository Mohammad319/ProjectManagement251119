using Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Users
{
    /// <summary>
    /// Persists a single personal list UI-setting for a user within a tenant:
    /// visible columns, column widths, saved filters, or saved column views.
    /// One row per (TenantId, UserId, Scope, Kind); <see cref="Payload"/> holds the
    /// JSON (same shape that was previously stored client-side in localStorage).
    /// TenantId is assigned automatically by the audit/tenant interceptor.
    /// </summary>
    public sealed class UserListSettingEntity : AuditableEntity<int>
    {
        /// <summary>The application user this setting belongs to.</summary>
        public int UserId { get; private set; }

        /// <summary>Which list the setting belongs to, e.g. "ProjectList" | "CalculationList".</summary>
        [Required]
        [MaxLength(60)]
        public string Scope { get; private set; } = string.Empty;

        /// <summary>Setting kind, e.g. "VisibleColumns" | "ColumnWidths" | "SavedFilters" | "SavedColumnViews".</summary>
        [Required]
        [MaxLength(60)]
        public string Kind { get; private set; } = string.Empty;

        /// <summary>Serialized JSON payload.</summary>
        [Required]
        public string Payload { get; private set; } = "[]";

        private UserListSettingEntity() { }

        public static UserListSettingEntity Create(int userId, string scope, string kind, string? payload)
        {
            return new UserListSettingEntity
            {
                UserId = userId,
                Scope = Normalize(scope, nameof(scope), 60),
                Kind = Normalize(kind, nameof(kind), 60),
                Payload = string.IsNullOrWhiteSpace(payload) ? "[]" : payload
            };
        }

        public void SetPayload(string? payload)
        {
            Payload = string.IsNullOrWhiteSpace(payload) ? "[]" : payload;
        }

        private static string Normalize(string? value, string fieldName, int maxLength)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ValidationException($"{fieldName} is required.");

            return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
        }
    }
}
