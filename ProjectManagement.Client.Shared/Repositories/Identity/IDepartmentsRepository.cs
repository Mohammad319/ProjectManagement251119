
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.Models.Account;
using ProjectManagement.Shared.Base.Users;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Identity
{
    public interface IDepartmentsRepository
    {
        Task<List<ListDTO>> GetDepartmentsAsListAsync();

        //----------- Users
        Task<List<ListDTO>> GetUsersAsListAsync(int DepartmentId);

        /// <summary>
        /// Users in a department including their system role and department id — used by the
        /// project-sharing dialog to disable the "Användare" permission for system Visare and to
        /// mark recipients that already have access.
        /// </summary>
        Task<List<UserAuthModel>> GetUsersAuthAsListAsync(int DepartmentId);
    }
}
