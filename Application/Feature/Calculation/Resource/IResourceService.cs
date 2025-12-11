using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Calculation.Resource
{
    public interface IResourceService
    {
        Task<bool> CreateAsync(IReadOnlyList<ResourcePostDTO> items, int parentTaskId, CancellationToken ct);
        Task<bool> UpdateAsync(int id, ResourcePostDTO dto, CancellationToken ct);
        Task<bool> DeleteAsync(IEnumerable<int> ids, int calcId, CancellationToken ct);

        Task<bool> CopyAsync(IReadOnlyList<ResourceTaskItemDTO> items, int parentTaskId, int sourceCalcId, CancellationToken ct);
        Task<bool> CutAsync(int targetTaskId, int sourceCalcId, IReadOnlyList<ResourceTaskItemDTO> items, CancellationToken ct);
        Task<bool> NewOrderAsync(int id, double newOrder, CancellationToken ct);

    }
}
