using Application.Interfaces;
using Domain.Entities.Application;
namespace Application.Feature.Application.Queries;

public sealed record GetCalcAppQuery(int CalcId) : IRequest<IEnumerable<ApplicationValuesEntity>>;

public sealed class GetCalcAppQueryHandler(IShardingSingleDbContext _context)
    : IRequestHandler<GetCalcAppQuery, IEnumerable<ApplicationValuesEntity>>
{
    public async Task<IEnumerable<ApplicationValuesEntity>> Handle(GetCalcAppQuery request, CancellationToken cancellationToken)
    {
        return await _context.ApplicationValues
            .Include(y => y.Application)
            .Where(x => x.CalculationId == request.CalcId)
            .OrderByDescending(x => x)
            .ToListAsync(cancellationToken);
    }
}
