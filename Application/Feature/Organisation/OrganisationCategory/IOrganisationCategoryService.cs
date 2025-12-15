
using Domain.DTO.Category;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Feature.Organisation.OrganisationCategory
{
    public interface IOrganisationCategoryService
    {
        // Commands
        Task<int> CreateAsync(PostOrganisationCategoryDTO dto, CancellationToken ct = default);
        Task<bool> UpdateAsync(PutOrganisationCategoryDTO dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);

        // Queries
        Task<List<ListOrganisationCategoryDTO>> GetAllAsync(CancellationToken ct = default);
    }
}
