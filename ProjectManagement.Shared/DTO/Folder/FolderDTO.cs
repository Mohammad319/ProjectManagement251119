using ProjectManagement.Shared.Base.Project;
using System;

namespace ProjectManagement.Shared.DTO.Folder
{
    public class PostFolderDTO : FolderBase{    }

    public class DetailsFolderDTO : FolderBase {
        public string Department { get; set; } = string.Empty;
        public string CreateBy { get; set; } = string.Empty;
    }

    public class ListFolderDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public string Color { get; set; } = "#08bf66";
        public bool IsVisible { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Number of projects in the folder that match the current archived filter.
        /// Lets the tree decide whether a folder has children (and thus an expand
        /// chevron) before its projects have been lazily loaded.
        /// </summary>
        public int ProjectCount { get; set; }

        /// <summary>Department the folder belongs to. Used to group the tree in "Alla tillgängliga".</summary>
        public int DepartmentId { get; set; }

        /// <summary>Display name of the folder's department (for the "Alla tillgängliga" group header).</summary>
        public string? DepartmentName { get; set; }

        /// <summary>
        /// True when the folder is only a VISUAL GROUP for shared/assigned projects from a department
        /// the user has no normal access to. Such folders are read-only: they cannot be created in,
        /// edited, moved, copied, archived or deleted, and projects cannot be created/imported into them.
        /// </summary>
        public bool IsSharedGroup { get; set; }
    }
}
