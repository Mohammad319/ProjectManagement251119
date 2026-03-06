using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.General;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation.Implement
{
    public class ResourceRepository(HTTPRepository httpsClient) : IResourceRepository
    {
        readonly HTTPRepository _httpRepository = httpsClient;
        static string ResourceURLBase => PMAPIConst.Resource;
        public async Task<bool> ReOrderAsync(int Id, int newOrder)
        {
            return await _httpRepository.GetAsync<bool>(ResourceURLBase + URLConst.ReOrder + $"/{Id}/{newOrder}");
        }
        public async Task<bool> CopyAsync(List<int> resources, int calcID, int taskId)
        {
            return await _httpRepository.PostAsync<bool, List<int>>(resources, ResourceURLBase + URLConst.Copy + $"/{calcID}/{taskId}");
        }
        public async Task<bool> CutAsync(List<int> resources, int calcID, int taskId)
        {
            return await _httpRepository.PostAsync<bool, List<int>>(resources, ResourceURLBase + URLConst.Cut + $"/{calcID}/{taskId}");
        }
        public async Task<bool> CreateAsync(List<ResourcePostDTO> models, int taskId)
        {
            return await _httpRepository.PostAsync<bool, List<ResourcePostDTO>>(models, ResourceURLBase + taskId);
        }

        public async Task<bool> UpdateAsync(ResourcePostDTO model, int id)
        {
            return await _httpRepository.PutAsync(model, ResourceURLBase + id);
        }
        public async Task<bool> DeleteAsync(int calcID, IEnumerable<int> items) => await _httpRepository.DeleteAsync(ResourceURLBase + calcID, items);

        public async Task<List<ResourceListMVVM>> GetByFilterAsync(FilterCalculationItemsDto res) =>
            await _httpRepository.PostAsync<List<ResourceListMVVM>, FilterCalculationItemsDto>(res, ResourceURLBase + URLConst.Filter);

        public async Task<ResourceFormDTO> GetConfigForm()
        {
            return await _httpRepository.GetAsync<ResourceFormDTO>(ResourceURLBase + $"");
        }
    }
}
