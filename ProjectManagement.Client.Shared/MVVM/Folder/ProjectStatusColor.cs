using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Shared.MVVM.Folder
{
    /// <summary>
    /// Single source of truth for project status colors.
    /// Used by both the project list (right panel) and the folder tree's
    /// status left-line so a project always shows the same status color
    /// everywhere. The color stored on the project status (admin settings /
    /// database) wins; archived projects and projects without any color get
    /// a neutral gray fallback.
    /// </summary>
    public static class ProjectStatusColor
    {
        public const string NeutralFallback = "#94a3b8";
        public const string ActiveFallback = "#10b981";

        public static string Resolve(ListProjectDTO project)
        {
            if (!string.IsNullOrWhiteSpace(project.Color))
                return project.Color;

            return project.IsArchived ? NeutralFallback : ActiveFallback;
        }
    }
}
