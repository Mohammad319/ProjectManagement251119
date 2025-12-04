using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Account;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Account
{
    public class AccountGroupsRepository(HTTPRepository _httpRepository) : IAccountGroupsRepository
    {
        static string AccountURLBase => PMAPIConst.Account;

        public async Task<List<AccountGroupModel>> GetAllAsync()
        {
            return await _httpRepository.GetAsync<List<AccountGroupModel>>(AccountURLBase + URLConst.Account.GetAll);
        }
    }
}
