using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Organisation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Feature.Organisation.Organisation
{
    public interface IOrganisationService
    {
        // ---------------- Commands ----------------
        Task<int> CreateAsync(PostOrganisationDTO dto, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, PostOrganisationDTO dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);

        // ---------------- Queries ----------------
        Task<OrganisationDetailsDTO?> GetDetailsAsync(int id, CancellationToken ct = default);
        Task<PostOrganisationDTO?> GetPostAsync(int id, CancellationToken ct = default);

        Task<List<ShortListOrganisationDTO>> GetByCategoryAsync(int categoryId, bool isVisible, CancellationToken ct = default);
        Task<List<ListDTO>> GetVisibleOrIdAsync(int? id, CancellationToken ct = default);
        Task<List<ListDTO>> GetAsListAsync(CancellationToken ct = default);
    }

}
