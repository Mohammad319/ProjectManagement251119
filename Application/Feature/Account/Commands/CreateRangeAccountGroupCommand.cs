using Application.Interfaces;
using AutoMapper;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Account;

namespace Application.Feature.Account.Commands;

public sealed record CreateRangeAccountGroupCommand(List<PostAccountGroupWithAccountsDTO> Items) : IRequest<List<int>>;

public sealed class CreateRangeAccountGroupCommandHandler(IShardingSingleDbContext _context, IMapper _mapper) : IRequestHandler<CreateRangeAccountGroupCommand, List<int>>
{
    public async Task<List<int>> Handle(CreateRangeAccountGroupCommand request, CancellationToken cancellationToken)
    {
        var entities = _mapper.Map<List<AccountGroupEntity>>(request.Items);
        _context.AccountGroup.AddRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
        return [.. entities.Select(e => e.Id)];
    }
}
