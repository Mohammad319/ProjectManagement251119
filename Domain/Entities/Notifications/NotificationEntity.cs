using Domain.Entities.Base;
using ProjectManagement.Shared.Enums;

namespace Domain.Entities.Notifications
{
    /// <summary>
    /// A notification targeted at ONE user (<see cref="UserId"/>) within the current tenant. Created
    /// e.g. when a project is shared with the user or their department, or when access is changed/removed.
    /// <para>
    /// Content is stored as STRUCTURED data (names, role, counts, validity) rather than a pre-formatted
    /// sentence, so the title/message can be rendered in the reader's own language (sv/en) at display time.
    /// </para>
    /// <para>
    /// The notification grants no access by itself – it is purely informational. When the user clicks
    /// "Open project" the backend must always re-check the current access. The audit field <c>CreatedBy</c>
    /// represents the actor (the user who performed the share). Unread = <see cref="ReadAt"/> is null.
    /// </para>
    /// </summary>
    public sealed class NotificationEntity : AuditableEntity<int>
    {
        /// <summary>The recipient (the user the notification is shown to).</summary>
        public int UserId { get; private set; }

        public NotificationType Type { get; private set; }

        /// <summary>Related project (when relevant).</summary>
        public Guid? ProjectId { get; private set; }

        /// <summary>Related calculation (when relevant).</summary>
        public int? CalculationId { get; private set; }

        /// <summary>Snapshot of the project name so the notification can be shown even if the project is later removed.</summary>
        public string? ProjectName { get; private set; }

        /// <summary>Display name of the user who performed the share (for "X shared … with you").</summary>
        public string? ActorName { get; private set; }

        /// <summary>Department name for department shares.</summary>
        public string? DepartmentName { get; private set; }

        /// <summary>Effective share role (a <c>PMRolesConst.Tenant.*</c> value), capped for system Visare. Drives the access-level label.</summary>
        public string? Role { get; private set; }

        /// <summary>Number of calculations included in the share.</summary>
        public int CalcCount { get; private set; }

        /// <summary>True when the share covers all shareable calculations of the project.</summary>
        public bool CalcAllAvailable { get; private set; }

        /// <summary>Share validity end date. <see langword="null"/> = indefinite.</summary>
        public DateTime? ValidUntil { get; private set; }

        /// <summary>When the notification was read. <see langword="null"/> = unread.</summary>
        public DateTime? ReadAt { get; private set; }

        /// <summary>When the user removed/hid the notification. <see langword="null"/> = still visible.</summary>
        public DateTime? DeletedAt { get; private set; }

        /// <summary>True once the user has removed/hidden the notification (kept internally for history).</summary>
        public bool IsDeleted => DeletedAt is not null;

        private NotificationEntity() { }

        public static NotificationEntity Create(
            int userId,
            NotificationType type,
            Guid? projectId = null,
            int? calculationId = null,
            string? projectName = null,
            string? actorName = null,
            string? departmentName = null,
            string? role = null,
            int calcCount = 0,
            bool calcAllAvailable = false,
            DateTime? validUntil = null)
        {
            return new NotificationEntity
            {
                UserId = userId,
                Type = type,
                ProjectId = projectId,
                CalculationId = calculationId,
                ProjectName = Trim(projectName, 256),
                ActorName = Trim(actorName, 200),
                DepartmentName = Trim(departmentName, 200),
                Role = Trim(role, 64),
                CalcCount = calcCount < 0 ? 0 : calcCount,
                CalcAllAvailable = calcAllAvailable,
                ValidUntil = validUntil?.Date
            };
        }

        /// <summary>Marks the notification as read. Idempotent – an already-read notification is left unchanged.</summary>
        public void MarkRead(DateTime now)
        {
            if (ReadAt is null)
                ReadAt = now;
        }

        /// <summary>
        /// Soft-removes the notification from the user's list. Implicitly marks it read first so the unread
        /// count stays correct. Idempotent – an already-removed notification is left unchanged.
        /// </summary>
        public void MarkDeleted(DateTime now)
        {
            if (ReadAt is null)
                ReadAt = now;

            if (DeletedAt is null)
                DeletedAt = now;
        }

        private static string? Trim(string? value, int maxLength)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
        }
    }
}
