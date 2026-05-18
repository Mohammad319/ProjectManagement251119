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

        public async Task<List<TaskResourceSuggestionDTO>> GetSuggestionsAsync(
            int taskId,
            int maxResults = 5,
            bool includeResources = true)
        {
            return await _httpRepository.GetAsync<List<TaskResourceSuggestionDTO>>(
                ResourceURLBase + $"suggestions/{taskId}?maxResults={maxResults}&includeResources={includeResources}");
        }

        public async Task<List<ResourcePostDTO>> GetSuggestionResourcesAsync(
            int sourceTaskId,
            TaskResourceSuggestionSource source)
        {
            return await _httpRepository.GetAsync<List<ResourcePostDTO>>(
                ResourceURLBase + $"suggestions/resources/{sourceTaskId}?source={source}");
        }

        public async Task<bool> RecordSuggestionFeedbackAsync(TaskResourceSuggestionFeedbackDTO feedback)
        {
            return await _httpRepository.PostAsync<bool, TaskResourceSuggestionFeedbackDTO>(
                feedback,
                ResourceURLBase + "suggestions/feedback");
        }

        public async Task<bool> UpdateAsync(ResourcePostDTO model, int id)
        {
            return await _httpRepository.PutAsync(model, ResourceURLBase + id);
        }
        public async Task<bool> DeleteAsync(int calcID, IEnumerable<int> items) => await _httpRepository.DeleteAsync(ResourceURLBase + calcID, items);

        public async Task<List<ResourceListMVVM>> GetByFilterAsync(FilterCalculationItemsDto res)
        {
            var dtos = await _httpRepository.PostAsync<List<ResourceListDTO>, FilterCalculationItemsDto>(res, ResourceURLBase + URLConst.Filter);
            return dtos.Select(x => x.ToResourceListMVVM()).ToList();
        }

        public async Task<ResourceFormDTO> GetConfigForm()
        {
            return await _httpRepository.GetAsync<ResourceFormDTO>(ResourceURLBase + $"");
        }

        public async Task<List<ListOrderDTO>> GetStatusesAsync(int? id = null)
        {
            return await _httpRepository.GetAsync<List<ListOrderDTO>>(PMAPIConst.ResourceStatus + $"?id={id}");
        }
    }
}
