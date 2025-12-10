using Application.Interfaces;
using Domain.Entities.Application;

namespace Application.Feature.Application.Queries;

public sealed record GetApplicationQuery(bool WithNoneVisible) : IRequest<List<ApplicationEntity>>;

public sealed class GetApplicationQueryHandler(IShardingSingleDbContext _context)
    : IRequestHandler<GetApplicationQuery, List<ApplicationEntity>>
{
    public async Task<List<ApplicationEntity>> Handle(GetApplicationQuery request, CancellationToken cancellationToken)
    {
        if (request.WithNoneVisible)
        {
            return await _context.Applications
                .OrderByDescending(x => x)
                .Where(x => x.IsVisible == true)
                .ToListAsync(cancellationToken);
        }

        return await _context.Applications
            .OrderByDescending(x => x)
            .ToListAsync(cancellationToken);
    }
}
