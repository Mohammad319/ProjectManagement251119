using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.General;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation
{
    public interface IResourceRepository
    {
        Task<ResourceFormDTO> GetConfigForm();
        Task<List<ListOrderDTO>> GetStatusesAsync(int? id = null);

        Task<bool> ReOrderAsync(int Id, int newOrder);
        Task<bool> CreateAsync(List<ResourcePostDTO> models, int taskId);
        Task<List<ResourceListMVVM>> GetByFilterAsync(FilterCalculationItemsDto offer);
        Task<bool> UpdateAsync(ResourcePostDTO model, int id);
        Task<bool> DeleteAsync(int calcID, IEnumerable<int> items);
    }
}
