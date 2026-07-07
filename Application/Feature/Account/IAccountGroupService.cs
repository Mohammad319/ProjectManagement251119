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
        /// ones depending on <paramref name="updateExisting"/>. Also records an import batch
        /// (file name, import type, created ids, row snapshot) so the import shows up under
        /// "Senaste importer" and can be undone when it only created new rows.</summary>
        Task<AccountImportResultDTO> ImportAsync(List<PostAccountGroupWithAccountsDTO> items, bool updateExisting, AccountImportBatchInfoDTO batchInfo, CancellationToken ct = default);

        /// <summary>Latest saved imports, newest first.</summary>
        Task<List<AccountImportBatchDTO>> GetImportBatchesAsync(int take = 10, CancellationToken ct = default);

        /// <summary>Row snapshot stored with a saved import ("Visa importerade rader").</summary>
        Task<List<AccountImportBatchRowDTO>> GetImportBatchRowsAsync(int batchId, CancellationToken ct = default);

        /// <summary>Undo an import that only created new rows: deletes the accounts the batch created
        /// (unless they are used by resources) and the groups it created that no longer hold accounts.
        /// Returns false when the batch cannot be undone (already undone, or it updated existing accounts).</summary>
        Task<bool> UndoImportBatchAsync(int batchId, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, PostAccountGroupDTO dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
