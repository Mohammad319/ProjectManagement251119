using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Client.Shared.Repositories.ResourceType;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.ResourceType;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation.Implement
{
    public class ResourceTypeRepository(HTTPRepository _httpRepository) : IResourceTypeRepository
    {
        static string ResourceTypeURLBase => PMAPIConst.ResourceType;
        static List<ListResourceTypeDTO> resourceTypes;
        public async Task<List<ListResourceTypeDTO>> GetLocalAsync()
        {
            if (resourceTypes == null)
                await GetVisualResourcesAsync();
            return resourceTypes;
        }
        public async Task<List<ListResourceTypeDTO>> GetVisualResourcesAsync()
        {
            resourceTypes = [.. (await _httpRepository.GetAsync<List<ListResourceTypeDTO>>(ResourceTypeURLBase + URLConst.GetAll)).OrderBy(x => x.Order)];
            return resourceTypes;
        }
        public async Task<List<ResourceSortModel>> GetResourceSortAsync(int resourceId)
        {
            return await _httpRepository.GetAsync<List<ResourceSortModel>>(ResourceTypeURLBase + URLConst.ResourceType.Sort + $"/{resourceId}");
        }
    }
}
