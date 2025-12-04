using Application.Interfaces;

namespace Application.Feature.Account.Commands;

public sealed record DeleteAccountCommand(int Id) : IRequest<bool>;

public sealed class DeleteAccountCommandHandler(IShardingSingleDbContext _context) : IRequestHandler<DeleteAccountCommand, bool>
{
    public async Task<bool> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Account.FindAsync([request.Id], cancellationToken);
        if (entity is null) return false;

        _context.Account.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
