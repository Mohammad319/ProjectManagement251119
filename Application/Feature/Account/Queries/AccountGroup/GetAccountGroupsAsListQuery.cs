using Application.Interfaces;
using ProjectManagement.Shared.DTO.Account;

namespace Application.Feature.Account.Queries.AccountGroup;

public sealed record GetAccountGroupsAsListQuery() : IRequest<List<ListAccountGroupIncludeAccountDTO>>;

public sealed class GetAccountGroupsAsListQueryHandler(IShardingSingleDbContext _context) : IRequestHandler<GetAccountGroupsAsListQuery, List<ListAccountGroupIncludeAccountDTO>>
{
    public async Task<List<ListAccountGroupIncludeAccountDTO>> Handle(GetAccountGroupsAsListQuery request, CancellationToken cancellationToken)
    {
        return await _context.AccountGroup.AsNoTracking()
            .Select(x => new ListAccountGroupIncludeAccountDTO
            {
                Id = x.Id,
                Name = x.Name,
                Accounts = x.Accounts
                    .OrderByDescending(y => y)
                    .Select(y => new ListAccountDTO
                    {
                        Account = y.Account,
                        Name = y.Name,
                        Id = y.Id,
                    }).ToList(),
            }).ToListAsync(cancellationToken);
    }
}
