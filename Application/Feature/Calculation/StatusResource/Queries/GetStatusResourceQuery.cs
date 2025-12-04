using Application.Interfaces;
using Domain.Entities.Calculation;

namespace Application.Feature.Calculation.StatusResource.Queries;

public sealed record GetStatusResourceQuery() : IRequest<List<StatusResourcesEntity>>;

public sealed class GetStatusResourceQueryHandler(IShardingSingleDbContext _context)
    : IRequestHandler<GetStatusResourceQuery, List<StatusResourcesEntity>>
{
    public async Task<List<StatusResourcesEntity>> Handle(GetStatusResourceQuery request, CancellationToken cancellationToken)
    {
        return await _context.ResourceStatus
            .AsNoTracking()
            .OrderByDescending(x => x)
            .ToListAsync(cancellationToken);
    }
}
