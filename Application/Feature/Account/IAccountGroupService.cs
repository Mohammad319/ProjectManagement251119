using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Account
{
    public interface IAccountGroupService
    {
        Task<List<ListDTO>> GetGroupsAsListAsync(CancellationToken ct = default); // dropdown
        Task<List<ListAccountGroupIncludeAccountDTO>> GetGroupsWithAccountsAsync(CancellationToken ct = default);

        Task<int> CreateAsync(PostAccountGroupDTO dto, CancellationToken ct = default);
        Task<List<int>> CreateRangeAsync(List<PostAccountGroupWithAccountsDTO> items, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, PostAccountGroupDTO dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
