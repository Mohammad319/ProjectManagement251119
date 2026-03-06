using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.General;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation
{
    public interface ITaskRepository
    {
        Task<List<ListOrderDTO>> GetAsync(int? id = null);
        Task<bool> ReOrderAsync(int Id, int newOrder);
        Task<bool> CreateAsync(List<TaskPostDTO> model, int NetCalc);
        Task<List<TaskListMVVM>> GetByFilterAsync(FilterCalculationItemsDto offer);
        Task<bool> UpdateAsync(TaskPostDTO model, int id);
        Task<bool> DeleteAsync(int calcID, IEnumerable<int> items);
    }
}
