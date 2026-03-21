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
    }
}
