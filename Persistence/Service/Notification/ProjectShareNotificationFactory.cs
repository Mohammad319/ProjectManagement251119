using Domain.Entities.Notifications;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;

namespace Persistence.Service.Notification
{
    /// <summary>
    /// Builds structured <see cref="NotificationEntity"/> instances for project-share events. No display
    /// text is produced here – titles/messages are rendered per reader-language at display time from the
    /// stored structured fields.
    /// </summary>
    internal static class ProjectShareNotificationFactory
    {
        /// <summary>Relative access strength so we can skip notifying users who already have an equal/stronger share.</summary>
        public static int RoleRank(string? role) => role switch
        {
            PMRolesConst.Tenant.Admin => 2,
            PMRolesConst.Tenant.Manger => 2,
            PMRolesConst.Tenant.Viewer => 1,
            _ => 0
        };

        /// <summary>A project was shared (directly with the user, or via a department).</summary>
        public static NotificationEntity Shared(
            int userId, Guid projectId, string projectName, string? actorName, string? departmentName,
            string role, int calcCount, bool calcAllAvailable, DateTime? validUntil, bool viaDepartment)
        {
            return NotificationEntity.Create(
                userId,
                viaDepartment ? NotificationType.ProjectSharedWithDepartment : NotificationType.ProjectSharedWithUser,
                projectId, null, projectName,
                viaDepartment ? null : actorName,
                viaDepartment ? departmentName : null,
                role, calcCount, calcAllAvailable, validUntil);
        }

        public static NotificationEntity AccessChanged(
            int userId, Guid projectId, string projectName, string role, int calcCount, bool calcAllAvailable, DateTime? validUntil)
            => NotificationEntity.Create(userId, NotificationType.ProjectAccessChanged, projectId, null, projectName,
                null, null, role, calcCount, calcAllAvailable, validUntil);

        public static NotificationEntity ValidityChanged(
            int userId, Guid projectId, string projectName, string role, int calcCount, bool calcAllAvailable, DateTime? validUntil)
            => NotificationEntity.Create(userId, NotificationType.ProjectShareValidityChanged, projectId, null, projectName,
                null, null, role, calcCount, calcAllAvailable, validUntil);

        public static NotificationEntity CalculationsChanged(
            int userId, Guid projectId, string projectName, string role, int calcCount, bool calcAllAvailable, DateTime? validUntil)
            => NotificationEntity.Create(userId, NotificationType.ProjectSharedCalculationsChanged, projectId, null, projectName,
                null, null, role, calcCount, calcAllAvailable, validUntil);

        public static NotificationEntity AccessRemoved(int userId, Guid projectId, string projectName)
            => NotificationEntity.Create(userId, NotificationType.ProjectAccessRemoved, projectId, null, projectName);
    }
}
