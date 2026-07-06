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

        /// <summary>Upsert import for the Kontoplan: reuses existing groups by name and creates missing ones,
        /// then creates missing accounts (by code within the group) and either skips or updates existing
        /// ones depending on <paramref name="updateExisting"/>.</summary>
        Task<AccountImportResultDTO> ImportAsync(List<PostAccountGroupWithAccountsDTO> items, bool updateExisting, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, PostAccountGroupDTO dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
