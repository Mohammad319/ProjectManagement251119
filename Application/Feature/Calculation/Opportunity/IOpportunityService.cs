using Domain.Entities.Calculation;
using Application.Feature.Calculation.Opportunity;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Services.CalculationItems.Opportunity
{
    public interface IOpportunityService
    {
        Task<int> CreateAsync(PostOpportunityDTO dto, int calculationId, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, PostOpportunityDTO dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);

        Task<List<OpportunityEntity>> GetByCalculationAsync(int calculationId, CancellationToken ct = default);
        Task<IReadOnlyList<OpportunityListItemDto>> GetListByCalculationAsync(int calculationId, CancellationToken ct = default);
    }
}
