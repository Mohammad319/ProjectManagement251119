using Application.Interfaces;
using Application.Services.CalculationItems.Opportunity;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Opportunity.Queries
{
    public sealed record GetOpportunitiesQuery(int CalculationId) : IRequest<List<OpportunityListDTO>>;

    public sealed class GetOpportunitiesQueryHandler(IOpportunityService service)
                : IRequestHandler<GetOpportunitiesQuery, List<OpportunityListDTO>>
    {
        public Task<List<OpportunityListDTO>> Handle(GetOpportunitiesQuery request, CancellationToken ct)
            => service.GetByCalculationAsync(request.CalculationId, ct);
    }
}
