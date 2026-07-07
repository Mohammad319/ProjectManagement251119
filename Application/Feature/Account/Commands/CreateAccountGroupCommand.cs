using Application.Interfaces;
using ProjectManagement.Shared.DTO.Account;

namespace Application.Feature.Account.Commands
{
    public sealed record CreateAccountGroupCommand(PostAccountGroupDTO Dto) : IRequest<int>;

    public sealed class CreateAccountGroupCommandHandler(IAccountGroupService service)
        : IRequestHandler<CreateAccountGroupCommand, int>
    {
        public Task<int> Handle(CreateAccountGroupCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, ct);
    }

    public sealed record CreateRangeAccountGroupCommand(List<PostAccountGroupWithAccountsDTO> Items) : IRequest<List<int>>;

    public sealed class CreateRangeAccountGroupCommandHandler(IAccountGroupService service)
        : IRequestHandler<CreateRangeAccountGroupCommand, List<int>>
    {
        public Task<List<int>> Handle(CreateRangeAccountGroupCommand request, CancellationToken ct)
            => service.CreateRangeAsync(request.Items, ct);
    }

    public sealed record ImportAccountGroupsCommand(List<PostAccountGroupWithAccountsDTO> Items, bool UpdateExisting, AccountImportBatchInfoDTO BatchInfo)
        : IRequest<AccountImportResultDTO>;

    public sealed class ImportAccountGroupsCommandHandler(IAccountGroupService service)
        : IRequestHandler<ImportAccountGroupsCommand, AccountImportResultDTO>
    {
        public Task<AccountImportResultDTO> Handle(ImportAccountGroupsCommand request, CancellationToken ct)
            => service.ImportAsync(request.Items, request.UpdateExisting, request.BatchInfo, ct);
    }

    public sealed record UndoAccountImportBatchCommand(int BatchId) : IRequest<bool>;

    public sealed class UndoAccountImportBatchCommandHandler(IAccountGroupService service)
        : IRequestHandler<UndoAccountImportBatchCommand, bool>
    {
        public Task<bool> Handle(UndoAccountImportBatchCommand request, CancellationToken ct)
            => service.UndoImportBatchAsync(request.BatchId, ct);
    }

    public sealed record UpdateAccountGroupCommand(PostAccountGroupDTO Dto, int Id) : IRequest<bool>;

    public sealed class UpdateAccountGroupCommandHandler(IAccountGroupService service)
        : IRequestHandler<UpdateAccountGroupCommand, bool>
    {
        public Task<bool> Handle(UpdateAccountGroupCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, ct);
    }

    public sealed record DeleteAccountGroupCommand(int Id) : IRequest<bool>;

    public sealed class DeleteAccountGroupCommandHandler(IAccountGroupService service)
        : IRequestHandler<DeleteAccountGroupCommand, bool>
    {
        public Task<bool> Handle(DeleteAccountGroupCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }
}
