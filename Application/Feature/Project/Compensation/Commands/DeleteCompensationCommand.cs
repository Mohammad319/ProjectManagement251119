using Application.Interfaces;

namespace Application.Feature.Project.Compensation.Commands
{
    public sealed record DeleteCompensationCommand(int Id) : IRequest<bool>;

    public class DeleteCompensationCommandHandler(IShardingSingleDbContext _context) : IRequestHandler<DeleteCompensationCommand, bool>
    {
        public async Task<bool> Handle(DeleteCompensationCommand request, CancellationToken cancellationToken)
        {
            var _ProjectTyp = await _context.Compensations.FindAsync(request.Id, cancellationToken);
            if (_ProjectTyp != null)
            {
                _context.Compensations.Remove(_ProjectTyp);
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false;
        }
    }
}
