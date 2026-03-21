using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.Mapping;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.General;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation.Implement
{
    public class TaskRepository(HTTPRepository _httpRepository) : ITaskRepository
    {
        static string TaskURLBase => PMAPIConst.Task;
        public async Task<List<ListOrderDTO>> GetAsync(int? id = null) =>
    [.. (await _httpRepository.GetAsync<List<ListOrderDTO>>(TaskURLBase + $"status?id={id}")).OrderBy(x => x.SortOrder),];

        public async Task<bool> ReOrderAsync(int Id, int newOrder)
        {
            return await _httpRepository.GetAsync<bool>(TaskURLBase + URLConst.ReOrder + $"/{Id}/{newOrder}");
        }

        public async Task<bool> CreateAsync(List<TaskPostDTO> model, int groupId)
        {
            return await _httpRepository.PostAsync<bool, List<TaskPostDTO>>(model, TaskURLBase + $"{groupId}");
        }
        public async Task<bool> UpdateAsync(TaskPostDTO model, int id)
        {
            return await _httpRepository.PutAsync(model, TaskURLBase + id);
        }
        public async Task<bool> DeleteAsync(int calcID, IEnumerable<int> items) => await _httpRepository.DeleteAsync(TaskURLBase + $"{calcID}", items);

        public async Task<List<TaskListMVVM>> GetByFilterAsync(FilterCalculationItemsDto task)
        {
            var dtos = await _httpRepository.PostAsync<List<TaskListDTO>, FilterCalculationItemsDto>(task, TaskURLBase + URLConst.Filter);
            return dtos.Select(x => x.ToTaskListMVVM()).ToList();
        }
    }
}
