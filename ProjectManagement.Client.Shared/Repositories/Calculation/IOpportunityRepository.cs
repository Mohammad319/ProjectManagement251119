using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation
{
    public interface IOpportunityRepository
    {
        Task<List<OpportunityModel>> GetAsync(int id);
        Task<int> CreateAsync(PostOpportunityDTO model, int calcultionId);
        Task<bool> UpdateAsync(PostOpportunityDTO model, int id);
        Task<bool> DeleteAsync(int id);
    }
}
