using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Calculation.Task
{
    public interface ITaskService
    {
        Task<bool> CopyAsync(
            IReadOnlyList<ResourceTaskItemDTO> taskItems,
            int? parentTaskId,
            int sourceCalcId,
            int targetCalcId,
            bool isOH,
            int userId,
            bool deleteOriginal = false,
            int? departmentId = null,
            CancellationToken cancellationToken = default);

        Task<List<TaskListDTO>> CreateAsync(
            IReadOnlyList<TaskPostDTO> tasks,
            int targetCalcId,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default);

        Task<bool> CutAsync(
            int sourceCalcId,
            int targetCalcId,
            int? parentTaskId,
            IReadOnlyList<ResourceTaskItemDTO> items,
            int userId,
            int order = 100,
            bool isOH = false,
            int? departmentId = null,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(
            IEnumerable<int> taskIds,
            int calcId,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default);

        Task<bool> NewOrderAsync(
            int taskId,
            int newOrder,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(
            int taskId,
            TaskPostDTO dto,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default);
    }
}
