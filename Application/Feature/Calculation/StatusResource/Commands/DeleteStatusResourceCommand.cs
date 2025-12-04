using Application.Interfaces;
using Application.Interfaces.Context;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Calculation.StatusResource.Commands
{
    public sealed record DeleteStatusResourceCommand(int Id) : IRequest<bool>;

    public class DeleteStatusResourceCommandHandler(IShardingSingleDbContext postRepository) : IRequestHandler<DeleteStatusResourceCommand, bool>
    {
        public async Task<bool> Handle(DeleteStatusResourceCommand request, CancellationToken cancellationToken)
        {
            var _StatusResource = await postRepository.ResourceStatus.FindAsync(request.Id);
            if (_StatusResource != null && !await postRepository.Resource.AnyAsync(x => x.StatusId == request.Id))
            {
                postRepository.ResourceStatus.Remove(_StatusResource);
                await postRepository.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }
}
