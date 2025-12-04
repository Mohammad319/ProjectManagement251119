using ProjectManagement.Shared.Base.Users;
using System;

namespace ProjectManagement.Shared.DTO.Identity
{
    public class DepartmentDetailsDTO : DepartmentBase
    {
        public int Id { get; set; }

        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? LastModified { get; set; }

        public int FoldersCount { get; set; }
        public int ProjectsCount { get; set; }
        public int UsersCount { get; set; }
    }
}
