using Application.Interfaces;
using Application.Interfaces.Context;
using AutoMapper;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Account;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Account.Commands;

public sealed record CreateAccountGroupCommand(PostAccountGroupDTO Dto) : IRequest<int>;

public sealed class CreateAccountGroupCommandHandler(IShardingSingleDbContext _context, IMapper _mapper) : IRequestHandler<CreateAccountGroupCommand, int>
{
    public async Task<int> Handle(CreateAccountGroupCommand request, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<AccountGroupEntity>(request.Dto);
        _context.AccountGroup.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
