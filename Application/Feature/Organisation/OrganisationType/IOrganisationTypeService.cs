
using Domain.DTO.Category;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Feature.Organisation.OrganisationType
{
    public interface IOrganisationTypeService
    {
        // Commands
        Task<int> CreateAsync(PostOrganisationTypeDTO dto, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, PostOrganisationTypeDTO dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);

        // Queries
        Task<List<ListOrganisationTypeDTO>> GetAllAsync(bool isVisible, CancellationToken ct = default);
        Task<List<ListDTO>> GetAsListAsync(int? typeId, int? organisationId, CancellationToken ct = default);
    }
}
