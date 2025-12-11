using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Calculation.TaskStatus
{
    public interface ITaskStatusQueryService
    {
        Task<List<ListOrderDTO>> GetAllAsync(CancellationToken ct = default);
        Task<ListOrderDTO?> GetByIdAsync(int id, CancellationToken ct = default);
    }
}
