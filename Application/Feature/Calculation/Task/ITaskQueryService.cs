using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Task
{
    public interface ITaskQueryService
    {
        Task<List<TaskListDTO>> GetByFilterAsync(
            FilterCalculationItemsDto filter,
            CancellationToken cancellationToken = default);
    }
}
