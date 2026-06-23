using System;
using System.Collections.Generic;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Shared.DTO.Notification
{
    /// <summary>
    /// A user notification as structured data. The displayed title/message is rendered from these fields
    /// in the reader's language (see the server-side notification presenter), not stored pre-formatted.
    /// </summary>
    public sealed class NotificationListItemDTO
    {
        public int Id { get; set; }
        public NotificationType Type { get; set; }

        /// <summary>Related project (when relevant). Used by "Open project".</summary>
        public Guid? ProjectId { get; set; }

        /// <summary>Related calculation (when relevant).</summary>
        public int? CalculationId { get; set; }

        /// <summary>Snapshot of the project name (still shown even if the project later becomes inaccessible).</summary>
        public string? ProjectName { get; set; }

        /// <summary>Display name of the user who performed the share.</summary>
        public string? ActorName { get; set; }

        /// <summary>Department name for department shares.</summary>
        public string? DepartmentName { get; set; }

        /// <summary>Effective share role (a <c>PMRolesConst.Tenant.*</c> value). Drives the access-level label.</summary>
        public string? Role { get; set; }

        /// <summary>Number of calculations included in the share.</summary>
        public int CalcCount { get; set; }

        /// <summary>True when the share covers all shareable calculations of the project.</summary>
        public bool CalcAllAvailable { get; set; }

        /// <summary>Share validity end date. <see langword="null"/> = indefinite.</summary>
        public DateTime? ValidUntil { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }

        /// <summary>Read when <see cref="ReadAt"/> has a value; unread otherwise.</summary>
        public bool IsRead => ReadAt.HasValue;
    }

    /// <summary>Summary for the notification icon: unread count + the most recent notifications.</summary>
    public sealed class NotificationSummaryDTO
    {
        public int UnreadCount { get; set; }
        public List<NotificationListItemDTO> Items { get; set; } = [];
    }

    /// <summary>
    /// Result of re-checking whether a notification's project can be opened. <see cref="CanOpen"/> is the
    /// authoritative access answer; <see cref="FolderId"/>/<see cref="DepartmentId"/> let the SPA navigate
    /// to and select the project in the tree.
    /// </summary>
    public sealed class ProjectOpenInfoDTO
    {
        public bool CanOpen { get; set; }
        public Guid? FolderId { get; set; }
        public int? DepartmentId { get; set; }
    }
}
