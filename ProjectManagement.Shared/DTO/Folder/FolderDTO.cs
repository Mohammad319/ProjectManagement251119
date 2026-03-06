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
    }
}
