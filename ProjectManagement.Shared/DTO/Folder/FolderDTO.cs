using ProjectManagement.Shared.Base.Project;
using System;

namespace ProjectManagement.Shared.DTO.Folder
{
    public class PostFolderDTO : FolderBase{    }

    public class DetailsFolderDTO : FolderBase {
        public string Department { get; set; }
        public string CreateBy { get; set; }
    }

    public class ListFolderDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public double Order { get; set; }
        public string Color { get; set; } = "#08bf66";
    }
}
