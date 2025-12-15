using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.ResourceType;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Feature.ResourceType
{
    public interface IResourceTypeService
    {
        // Commands
        Task<int> CreateTypeAsync(PostResourceTypeDTO dto, CancellationToken ct = default);
        Task<bool> UpdateTypeAsync(int id, PostResourceTypeDTO dto, CancellationToken ct = default);
        Task<bool> DeleteTypeAsync(int id, CancellationToken ct = default);

        Task<int> CreateSortAsync(int resourceTypeId, PostResourceSortDTO dto, CancellationToken ct = default);
        Task<bool> UpdateSortAsync(int id, PostResourceSortDTO dto, CancellationToken ct = default);
        Task<bool> DeleteSortAsync(int id, CancellationToken ct = default);

        // Queries
        Task<List<ResourceTypeModel>> GetTypesAsync(bool isVisible, CancellationToken ct = default);
        Task<List<ResourceSortModel>> GetSortsAsync(int resourceTypeId, CancellationToken ct = default);
        Task<ResourceFormDTO> GetVisualFormAsync(CancellationToken ct = default);
    }
}
