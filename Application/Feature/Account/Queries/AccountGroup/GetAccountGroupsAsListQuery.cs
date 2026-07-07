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
    public sealed record GetAccountQuery(int GroupId) : IRequest<List<AccountManageDTO>>;

    public sealed class GetAccountQueryHandler(IAccountService service)
        : IRequestHandler<GetAccountQuery, List<AccountManageDTO>>
    {
        public Task<List<AccountManageDTO>> Handle(GetAccountQuery request, CancellationToken ct)
            => service.GetAccountsByGroupAsync(request.GroupId, ct);
    }

    public sealed record GetAccountsOverviewQuery() : IRequest<List<AccountManageDTO>>;

    public sealed class GetAccountsOverviewQueryHandler(IAccountService service)
        : IRequestHandler<GetAccountsOverviewQuery, List<AccountManageDTO>>
    {
        public Task<List<AccountManageDTO>> Handle(GetAccountsOverviewQuery request, CancellationToken ct)
            => service.GetAccountsOverviewAsync(ct);
    }

    public sealed record GetAccountImportBatchesQuery(int Take = 10) : IRequest<List<AccountImportBatchDTO>>;

    public sealed class GetAccountImportBatchesQueryHandler(IAccountGroupService service)
        : IRequestHandler<GetAccountImportBatchesQuery, List<AccountImportBatchDTO>>
    {
        public Task<List<AccountImportBatchDTO>> Handle(GetAccountImportBatchesQuery request, CancellationToken ct)
            => service.GetImportBatchesAsync(request.Take, ct);
    }

    public sealed record GetAccountImportBatchRowsQuery(int BatchId) : IRequest<List<AccountImportBatchRowDTO>>;

    public sealed class GetAccountImportBatchRowsQueryHandler(IAccountGroupService service)
        : IRequestHandler<GetAccountImportBatchRowsQuery, List<AccountImportBatchRowDTO>>
    {
        public Task<List<AccountImportBatchRowDTO>> Handle(GetAccountImportBatchRowsQuery request, CancellationToken ct)
            => service.GetImportBatchRowsAsync(request.BatchId, ct);
    }
}
