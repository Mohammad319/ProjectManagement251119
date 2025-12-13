using Application.Interfaces;
using ProjectManagement.Shared.DTO.Account;

namespace Application.Feature.Account.Commands
{
    public sealed record CreateAccountCommand(PostAccountDTO AccountDto) : IRequest<int>;

    public sealed class CreateAccountCommandHandler(IAccountService service)
        : IRequestHandler<CreateAccountCommand, int>
    {
        public Task<int> Handle(CreateAccountCommand request, CancellationToken ct)
            => service.CreateAsync(request.AccountDto, ct);
    }

    public sealed record UpdateAccountCommand(PostAccountDTO Dto, int Id) : IRequest<bool>;

    public sealed class UpdateAccountCommandHandler(IAccountService service)
        : IRequestHandler<UpdateAccountCommand, bool>
    {
        public Task<bool> Handle(UpdateAccountCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, ct);
    }

    public sealed record DeleteAccountCommand(int Id) : IRequest<bool>;

    public sealed class DeleteAccountCommandHandler(IAccountService service)
        : IRequestHandler<DeleteAccountCommand, bool>
    {
        public Task<bool> Handle(DeleteAccountCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }
}
