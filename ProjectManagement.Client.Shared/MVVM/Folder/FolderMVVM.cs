using ProjectManagement.Shared.DTO.Folder;
using System.Collections.Generic;

namespace ProjectManagement.Client.Shared.MVVM.Folder
{
    public class FolderMVVM : ListFolderDTO
    {
        public bool IsDragOver = false;
        public bool ShowProjects = false;
        public bool Loading = false;
        public List<ListProjectMVVM> Projects { get; set; } = [];
        public bool ProjectsLoaded { get; set; }

        public bool IsLoading { get; set; } = false; // جديد

        /// <summary>
        /// True when this folder is a read-only VISUAL GROUP for shared/assigned projects from a
        /// department the user has no normal access to. The tree must not offer any folder-management
        /// action (create/edit/move/copy/archive/delete) or project create/import for such folders.
        /// Mirrors <see cref="ProjectManagement.Shared.DTO.Folder.ListFolderDTO.IsSharedGroup"/>.
        /// </summary>
        public bool IsReadOnlyGroup { get; set; }
    }
}
