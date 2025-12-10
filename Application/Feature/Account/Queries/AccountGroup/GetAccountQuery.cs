using Application.Interfaces;
using Domain.Entities.Calculation;

namespace Application.Feature.Account.Queries.AccountGroup
{
    public sealed record GetAccountQuery(int groupid) : IRequest<List<AccountEntity>>;

    public class GetAccountQueryHandler(IShardingSingleDbContext _context) : IRequestHandler<GetAccountQuery, List<AccountEntity>>
    {
        public async Task<List<AccountEntity>> Handle(GetAccountQuery query, CancellationToken cancellationToken)
        {
            return await _context.Accounts.Where(x=>x.AccountGroupId == query.groupid).OrderByDescending(x => x).ToListAsync(cancellationToken);
        }
    }
}
