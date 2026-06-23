namespace ProjectManagement.Shared.Enums
{
    /// <summary>
    /// Type of a user notification. Stored as <see cref="int"/> in the database. Notifications
    /// currently concern project sharing, but the enum is intentionally generic so more types can
    /// be added later.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>A project was shared directly with the user.</summary>
        ProjectSharedWithUser = 0,

        /// <summary>A project was shared with a department the user belongs to.</summary>
        ProjectSharedWithDepartment = 1,

        /// <summary>The user's access level changed (e.g. Can view → Can edit).</summary>
        ProjectAccessChanged = 2,

        /// <summary>The user's share/access was removed.</summary>
        ProjectAccessRemoved = 3,

        /// <summary>The share validity changed (end date / extension).</summary>
        ProjectShareValidityChanged = 4,

        /// <summary>The calculation selection in the share changed in a way that affects the user.</summary>
        ProjectSharedCalculationsChanged = 5
    }
}
