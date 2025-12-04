using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Organisation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Organisation
{
    public interface IOrganisationRepository
    {
        Task<List<ListDTO>> GetVisibleOrIdAsync(int? orgId = null);
    }
}
