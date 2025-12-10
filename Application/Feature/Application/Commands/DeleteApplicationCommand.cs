using Application.Interfaces;
using Application.Interfaces.Context;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Application.Commands
{
    public sealed record DeleteApplicationCommand(int Id) : IRequest<bool>;

    public class DeleteApplicationCommandHandler(IShardingSingleDbContext _context) : IRequestHandler<DeleteApplicationCommand, bool>
    {
        public async Task<bool> Handle(DeleteApplicationCommand request, CancellationToken cancellationToken)
        {
            var _folder = await _context.Applications.FindAsync(request.Id);
            if (_folder != null)
            {
                _context.Applications.Remove(_folder);
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false;
        }
    }
}
