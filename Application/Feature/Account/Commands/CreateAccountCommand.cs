using Application.Interfaces;
using AutoMapper;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Account;

namespace Application.Feature.Account.Commands
{
    /// <summary>
    /// Command to create a new account using PostAccountDTO.
    /// </summary>
    public sealed record CreateAccountCommand(PostAccountDTO AccountDto) : IRequest<int>;

    public sealed class CreateAccountCommandHandler(IShardingSingleDbContext _context, IMapper _mapper) : IRequestHandler<CreateAccountCommand, int>
    {
        public async Task<int> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<AccountEntity>(request.AccountDto);
            _context.Accounts.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }
    }
}
