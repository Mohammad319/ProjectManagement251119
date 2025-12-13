using Application.Interfaces;
using Application.Services.CalculationItems.Opportunity;
using Domain.Entities.Calculation;

namespace Application.Feature.Calculation.Opportunity.Queries
{
    public sealed record GetOpportunitiesQuery(int CalculationId) : IRequest<List<OpportunityEntity>>;

    public sealed class GetOpportunitiesQueryHandler(IOpportunityService service)
                : IRequestHandler<GetOpportunitiesQuery, List<OpportunityEntity>>
    {
        public Task<List<OpportunityEntity>> Handle(GetOpportunitiesQuery request, CancellationToken ct)
            => service.GetByCalculationAsync(request.CalculationId, ct);
    }
}
