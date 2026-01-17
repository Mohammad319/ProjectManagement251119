using ProjectManagement.Shared.DTO.Project;

namespace Application.Services.CalculationItems.Storage
{
    public interface IStorageQueryService
    {
        Task<IEnumerable<StorageDTO<object>>> GetAsync(
            CalculationItemType type,
            AuthorityStorage level,
            StorageSort sort,
            CancellationToken ct = default);
    }
}
