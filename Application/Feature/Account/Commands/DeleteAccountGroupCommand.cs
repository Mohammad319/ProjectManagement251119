using Application.Interfaces;

namespace Application.Feature.Account.Commands;

public sealed record DeleteAccountGroupCommand(int Id) : IRequest<bool>;

public sealed class DeleteAccountGroupCommandHandler(IShardingSingleDbContext _context) : IRequestHandler<DeleteAccountGroupCommand, bool>
{
    public async Task<bool> Handle(DeleteAccountGroupCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.AccountGroup.FindAsync([request.Id], cancellationToken);
        if (entity is null) return false;

        _context.AccountGroup.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
