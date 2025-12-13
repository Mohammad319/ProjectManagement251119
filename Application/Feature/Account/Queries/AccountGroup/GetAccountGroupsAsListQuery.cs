using Application.Interfaces;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Account.Queries
{
    public sealed record GetAccountGroupsAsListQuery() : IRequest<List<ListAccountGroupIncludeAccountDTO>>;

    public sealed class GetAccountGroupsAsListQueryHandler(IAccountGroupService service)
        : IRequestHandler<GetAccountGroupsAsListQuery, List<ListAccountGroupIncludeAccountDTO>>
    {
        public Task<List<ListAccountGroupIncludeAccountDTO>> Handle(GetAccountGroupsAsListQuery request, CancellationToken ct)
            => service.GetGroupsWithAccountsAsync(ct);
    }

    public sealed record GetAccountGroupsQuery() : IRequest<List<ListDTO>>;

    public sealed class GetAccountGroupsQueryHandler(IAccountGroupService service)
        : IRequestHandler<GetAccountGroupsQuery, List<ListDTO>>
    {
        public Task<List<ListDTO>> Handle(GetAccountGroupsQuery request, CancellationToken ct)
            => service.GetGroupsAsListAsync(ct);
    }
    public sealed record GetAccountQuery(int GroupId) : IRequest<List<ListAccountDTO>>;

    public sealed class GetAccountQueryHandler(IAccountService service)
        : IRequestHandler<GetAccountQuery, List<ListAccountDTO>>
    {
        public Task<List<ListAccountDTO>> Handle(GetAccountQuery request, CancellationToken ct)
            => service.GetAccountsByGroupAsync(request.GroupId, ct);
    }

}
