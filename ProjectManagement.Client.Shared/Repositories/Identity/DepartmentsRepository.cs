using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared.Base.Users;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.Models.Account;
using System.Collections.Generic;
using System.Linq;
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
            var users = await _httpRepository.GetAsync<List<UserAuthModel>>(DepartmentURLBase + URLConst.Department.GetUsers + $"/{DepartmentId}")
                ?? [];

            return [.. users
                .Where(x => x.UserId is > 0)
                .Select(x => new ListDTO
                {
                    Id = x.UserId!.Value,
                    Name = BuildDisplayName(x)
                })
                .OrderBy(x => x.Name)];
        }

        public async Task<List<UserAuthModel>> GetUsersAuthAsListAsync(int DepartmentId)
        {
            var users = await _httpRepository.GetAsync<List<UserAuthModel>>(DepartmentURLBase + URLConst.Department.GetUsers + $"/{DepartmentId}")
                ?? [];

            return [.. users
                .Where(x => x.UserId is > 0)
                .OrderBy(BuildDisplayName)];
        }

        private static string BuildDisplayName(UserAuthModel user)
        {
            var fullName = user.FullName.Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
                return fullName;

            if (!string.IsNullOrWhiteSpace(user.Email))
                return user.Email;

            if (!string.IsNullOrWhiteSpace(user.Username))
                return user.Username;

            return user.Id;
        }
    }
}
