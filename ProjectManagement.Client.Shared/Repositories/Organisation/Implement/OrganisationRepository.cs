using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Organisation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Organisation.Implement
{
    public class OrganisationRepository(HTTPRepository _httpRepository) : IOrganisationRepository
    {
        static string CompanyURLBase => PMAPIConst.Organisations;

        public async Task<List<ListDTO>> GetVisibleOrIdAsync(int? orgId = null)
        {
            if (orgId == null) orgId = 0;
            return await _httpRepository.GetAsync<List<ListDTO>>(CompanyURLBase + $"{URLConst.Company.GetVisibleOrByID}/{orgId}");
        }
    }
}
