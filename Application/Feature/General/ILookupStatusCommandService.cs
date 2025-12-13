using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.General
{
    public interface ILookupStatusCommandService<TEntity>
    {
        Task<int> CreateAsync(PostTaskStatusDTO dto, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, PostTaskStatusDTO dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
