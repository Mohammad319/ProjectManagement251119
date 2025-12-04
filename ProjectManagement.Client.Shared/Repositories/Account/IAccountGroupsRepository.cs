using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Shared.DTO.Account;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Account
{
    public interface IAccountGroupsRepository
    {
        Task<List<AccountGroupModel>> GetAllAsync();
    }
}
