namespace ProjectManagement.Client.Shared.Constants
{
    /// <summary>Constants for the folder workspace department dropdown / tree.</summary>
    public static class FolderConstants
    {
        /// <summary>
        /// Sentinel value used for the special "Alla tillgängliga" entry in the department dropdown.
        /// When selected, the tree shows every project the user may see across all departments
        /// (own/normal departments + projects shared/assigned from other departments).
        /// Negative so it never collides with a real department id.
        /// </summary>
        public const int AllAvailableDepartmentId = -1;
    }
}
