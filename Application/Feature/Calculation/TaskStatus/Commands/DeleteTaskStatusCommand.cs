using Application.Interfaces;
using Application.Interfaces.Context;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Calculation.TaskStatus.Commands
{
    public sealed record DeleteTaskStatusCommand(int Id) : IRequest<bool>;

    public class DeleteTaskStatusCommandHandler(IShardingSingleDbContext _dataAccess) : IRequestHandler<DeleteTaskStatusCommand, bool>
    {
        public async Task<bool> Handle(DeleteTaskStatusCommand request, CancellationToken cancellationToken)
        {
            var _TaskStatus = await _dataAccess.TaskStatus.FindAsync(request.Id);
            if (_TaskStatus != null && !await _dataAccess.Tasks.AnyAsync(x => x.StatusId == request.Id))
            {
                _dataAccess.TaskStatus.Remove(_TaskStatus);
                await _dataAccess.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false;
        }
    }
}
