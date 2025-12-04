using Application.Interfaces;
using Application.Interfaces.Context;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Calculation.Storage.Commands
{
    public sealed record DeleteStorageCommand(int Id, int UserId=0) : IRequest<bool>;

        public class CreateTaskCommandHandler(IShardingSingleDbContext _dataAccess) : IRequestHandler<DeleteStorageCommand, bool>
        {
            public async Task<bool> Handle(DeleteStorageCommand request, CancellationToken cancellationToken)
            {
                var task = await _dataAccess.Storage.FindAsync(request.Id);
                if (task == null)
                    return false;
                _dataAccess.Storage.Remove(task);
                await _dataAccess.SaveChangesAsync();
                return true;
            }
        }
    }
