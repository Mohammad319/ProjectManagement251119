using Application.Interfaces;
using Domain.Entities.Calculation;

namespace Application.Feature.Account.Queries.AccountGroup
{
    public sealed record GetAccountGroupsQuery() : IRequest<List<AccountGroupEntity>>;

    public class GetAccountGroupsQueryHandler(IShardingSingleDbContext _context) : IRequestHandler<GetAccountGroupsQuery, List<AccountGroupEntity>>
    {
        public async Task<List<AccountGroupEntity>> Handle(GetAccountGroupsQuery query, CancellationToken cancellationToken)
        {
            return await _context.AccountGroup
                .OrderByDescending(x => x).AsNoTracking().Select(x => new AccountGroupEntity()
                {
                    Name = x.Name,
                    Id = x.Id
                }).ToListAsync(cancellationToken);
        }
    }
}
