using Application.Interfaces;
using Application.Interfaces.Context;
using AutoMapper;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Application.Commands
{
    public sealed record DeleteCalcAppCommand(int Id) : IRequest<bool>;

    public class DeleteCalcAppCommandHandler(IShardingSingleDbContext _dataAccess) : IRequestHandler<DeleteCalcAppCommand, bool>
    {
        public async Task<bool> Handle(DeleteCalcAppCommand request, CancellationToken cancellationToken)
        {
            var _folder = await _dataAccess.ApplicationValues.FindAsync(request.Id);
            if (_folder != null)
            {
                _dataAccess.ApplicationValues.Remove(_folder);
                await _dataAccess.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false;
        }
    }
}
