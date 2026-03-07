using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared.Base.Users;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.Models.Account;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Identity
{
    public class DepartmentsRepository(HTTPRepository _httpRepository) : IDepartmentsRepository
    {
        static string DepartmentURLBase => PMAPIConst.Departments;
        public async Task<List<ListDTO>> GetDepartmentsAsListAsync()
        {
            return await _httpRepository.GetAsync<List<ListDTO>>(DepartmentURLBase);
        }
        //----------- Users
        public async Task<List<ListDTO>> GetUsersAsListAsync(int DepartmentId)
        {
            return await _httpRepository.GetAsync<List<ListDTO>>(DepartmentURLBase + URLConst.Department.GetUsers + $"/{DepartmentId}");
        }
    }
}
