using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Calculation.Resource
{
    public interface IResourceService
    {
        Task<bool> CreateAsync(IReadOnlyList<ResourcePostDTO> items, int parentTaskId, int userId, int? departmentId, CancellationToken ct);
        Task<bool> UpdateAsync(int id, ResourcePostDTO dto, int userId, int? departmentId, CancellationToken ct);
        Task<bool> DeleteAsync(IEnumerable<int> ids, int calcId, int userId, int? departmentId, CancellationToken ct);

        Task<bool> CopyAsync(IReadOnlyList<ResourceTaskItemDTO> items, int parentTaskId, int sourceCalcId, int userId, int? departmentId, CancellationToken ct);
        Task<bool> CutAsync(int targetTaskId, int sourceCalcId, IReadOnlyList<ResourceTaskItemDTO> items, int userId, int? departmentId, CancellationToken ct);
        Task<bool> NewOrderAsync(int id, int newOrder, int userId, int? departmentId, CancellationToken ct);

    }
}
