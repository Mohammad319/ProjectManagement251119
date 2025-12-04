using Application.Interfaces;
using AutoMapper;
using ProjectManagement.Shared.DTO.Account;

namespace Application.Feature.Account.Commands;

public sealed record UpdateAccountGroupCommand(PostAccountGroupDTO Dto, int Id) : IRequest<bool>;

public sealed class UpdateAccountGroupCommandHandler(IShardingSingleDbContext _context, IMapper _mapper) : IRequestHandler<UpdateAccountGroupCommand, bool>
{
    public async Task<bool> Handle(UpdateAccountGroupCommand request, CancellationToken cancellationToken)
    {
        var existing = await _context.AccountGroup.FindAsync(request.Id);
        if (existing is null) return false;
        _mapper.Map(request.Dto, existing);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
