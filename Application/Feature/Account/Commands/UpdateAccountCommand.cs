using Application.Interfaces;
using AutoMapper;
using ProjectManagement.Shared.DTO.Account;

namespace Application.Feature.Account.Commands;

public sealed record UpdateAccountCommand(PostAccountDTO Dto,int Id) : IRequest<bool>;

public sealed class UpdateAccountCommandHandler(IShardingSingleDbContext _context, IMapper _mapper) : IRequestHandler<UpdateAccountCommand, bool>
{

    public async Task<bool> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var existing = await _context.Account.FindAsync([request.Id], cancellationToken);
        if (existing is null) return false;
        _mapper.Map(request.Dto, existing);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
