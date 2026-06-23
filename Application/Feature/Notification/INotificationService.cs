using ProjectManagement.Shared.DTO.Notification;

namespace Application.Feature.Notification
{
    /// <summary>
    /// Read/maintenance operations for a user's notifications. Creation of notifications happens as a
    /// side effect of the triggering action (e.g. project sharing) and is therefore not exposed here.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>Unread count + the most recent notifications (unread first), for the bell/panel.</summary>
        Task<NotificationSummaryDTO> GetSummaryAsync(int userId, int take, CancellationToken ct = default);

        /// <summary>Paged notification list (unread first, then newest first).</summary>
        Task<IReadOnlyList<NotificationListItemDTO>> GetListAsync(int userId, int skip, int take, CancellationToken ct = default);

        /// <summary>Number of unread notifications for the user.</summary>
        Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default);

        /// <summary>Marks a single notification as read. Returns false if it does not belong to the user.</summary>
        Task<bool> MarkAsReadAsync(int id, int userId, CancellationToken ct = default);

        /// <summary>Marks all of the user's unread notifications as read. Returns the number affected.</summary>
        Task<int> MarkAllAsReadAsync(int userId, CancellationToken ct = default);

        /// <summary>
        /// Soft-removes a single notification from the user's list (also marks it read). Returns false if it
        /// does not belong to the user. History is kept internally; the notification is simply hidden.
        /// </summary>
        Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default);

        /// <summary>Soft-removes all of the user's read notifications ("clear read"). Returns the number affected.</summary>
        Task<int> ClearReadAsync(int userId, CancellationToken ct = default);

        /// <summary>
        /// Re-checks whether the user currently has access to the project (notifications never grant
        /// access) and returns where to find it in the tree. <c>CanOpen</c> is false if the share was
        /// removed or has expired.
        /// </summary>
        Task<ProjectOpenInfoDTO> GetProjectOpenInfoAsync(Guid projectId, int userId, int? departmentId, bool isViewer, CancellationToken ct = default);
    }
}
