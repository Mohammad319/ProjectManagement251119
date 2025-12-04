using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Shared.DTO.ResourceType;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.ResourceType
{
    public interface IResourceTypeRepository
    {
        Task<List<ListResourceTypeDTO>> GetLocalAsync();
        Task<List<ListResourceTypeDTO>> GetVisualResourcesAsync();
        Task<List<ResourceSortModel>> GetResourceSortAsync(int resourceId);
    }
}
