
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Identity;
using ProjectManagement.Shared.Models.Account;
using ProjectManagement.Shared.Base.Users;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Identity
{
    public interface IDepartmentsRepository
    {
        Task<List<DepartmentDetailsDTO>> GetDepartmentDetailsAsync();
        Task<List<ListDTO>> GetDepartmentsAsListAsync();

        //----------- Users
        Task<List<ListDTO>> GetUsersAsListAsync(int DepartmentId);
    }
}
