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
        public int CalculationsCount { get; set; }
        public int UsersCount { get; set; }

        /// <summary>Display name of the department head, resolved from <see cref="DepartmentBase.HeadUserId"/>.</summary>
        public string? HeadUserName { get; set; }
    }
}
