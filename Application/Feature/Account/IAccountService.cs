using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Account
{
    public interface IAccountService
    {
        Task<List<AccountManageDTO>> GetAccountsByGroupAsync(int groupId, CancellationToken ct = default);
        Task<List<AccountManageDTO>> GetAccountsOverviewAsync(CancellationToken ct = default); // whole Kontoplan as one table
        Task<List<ListDTO>> GetAccountsAsListAsync(int groupId, CancellationToken ct = default); // dropdown

        Task<int> CreateAsync(PostAccountDTO dto, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, PostAccountDTO dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
