using Application.Interfaces;

namespace Application.Feature.Project.Status.Commands
{
    public sealed record DeleteStatusCommand(int Id) : IRequest<bool>;

    public class DeleteStatusCommandHandler(IShardingSingleDbContext _context) : IRequestHandler<DeleteStatusCommand, bool>
    {
        public async Task<bool> Handle(DeleteStatusCommand request, CancellationToken cancellationToken)
        {
            var _status = await _context.CalculationStatus.FindAsync(request.Id, cancellationToken);
            if (_status != null)
            {
                _context.CalculationStatus.Remove(_status);
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false;
        }
    }
}
