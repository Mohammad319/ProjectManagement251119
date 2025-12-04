using Application.Interfaces;
using Domain.Entities.Calculation;

namespace Application.Feature.Calculation.Opportunity.Queries;

public sealed record GetOpportunitiesQuery(int CalculationId) : IRequest<List<OpportunityEntity>>;

public sealed class GetOpportunitiesQueryHandler(IShardingSingleDbContext _context)
    : IRequestHandler<GetOpportunitiesQuery, List<OpportunityEntity>>
{
    public async Task<List<OpportunityEntity>> Handle(GetOpportunitiesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Opportunity
            .Where(x => x.CalculationId == request.CalculationId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
