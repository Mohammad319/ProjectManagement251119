using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.Mapping;
using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation.Implement
{
    public class OpportunityRepository(HTTPRepository httpsClient) : IOpportunityRepository
    {
        readonly HTTPRepository _httpRepository = httpsClient;
        static string OpportunityURLBase => "api/v1/opportunity/";
        public async Task<List<OpportunityModel>> GetAsync(int id)
        {
            var dtos = await _httpRepository.GetAsync<List<OpportunityListDTO>>(OpportunityURLBase + id);
            return dtos.Select(x => x.ToOpportunityModel()).ToList();
        }
        public async Task<int> CreateAsync(PostOpportunityDTO model, int calcultionId)
        {
            return await _httpRepository.PostAsync<int, PostOpportunityDTO>(model, OpportunityURLBase + calcultionId);
        }
        public async Task<bool> UpdateAsync(PostOpportunityDTO model, int id)
        {
            return await _httpRepository.PutAsync(model, OpportunityURLBase + id);
        }
        public async Task<bool> DeleteAsync(int id)
        {
            return await _httpRepository.DeleteAsync(OpportunityURLBase + id);
        }
    }
}
