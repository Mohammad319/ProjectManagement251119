using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Client.Shared.Repositories.ResourceType;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.ResourceType;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation.Implement
{
    public class ResourceTypeRepository(HTTPRepository _httpRepository) : IResourceTypeRepository
    {
        static string ResourceURLBase => PMAPIConst.Resource;
        private List<ListResourceTypeDTO>? _resourceTypes;

        public async Task<List<ListResourceTypeDTO>> GetLocalAsync()
        {
            await EnsureLoadedAsync();
            return _resourceTypes ?? [];
        }

        public async Task<List<ListResourceTypeDTO>> GetVisualResourcesAsync()
        {
            await EnsureLoadedAsync();
            return _resourceTypes ?? [];
        }

        public async Task<List<ResourceSortModel>> GetResourceSortAsync(int resourceId)
        {
            await EnsureLoadedAsync();

            return _resourceTypes?
                .FirstOrDefault(x => x.Id == resourceId)?
                .ResourcesSort?
                .OrderBy(x => x.Order)
                .Select(x => new ResourceSortModel
                {
                    Id = x.Id,
                    ResourceTypeId = resourceId,
                    AccountId = x.AccountId,
                    Name = x.Name,
                    Order = x.Order,
                    IsVisible = x.IsVisible,
                    Data = x.Data
                })
                .ToList() ?? [];
        }

        private async Task EnsureLoadedAsync()
        {
            if (_resourceTypes != null)
                return;

            var config = await _httpRepository.GetAsync<ResourceFormDTO>(ResourceURLBase);
            _resourceTypes = [.. (config.ResourceTypes ?? []).OrderBy(x => x.Order)];
        }
    }
}
