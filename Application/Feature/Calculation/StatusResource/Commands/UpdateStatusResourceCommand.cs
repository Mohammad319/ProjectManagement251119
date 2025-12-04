using Application.Interfaces;
using Application.Interfaces.Context;
using ProjectManagement.Shared.DTO.Calculation;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Calculation.StatusResource.Commands
{
    public sealed record UpdateStatusResourceCommand(int Id, PostResourceStatusDTO dto) : IRequest<bool>;

    public class EditStatusResourceCommandHandler(IShardingSingleDbContext postRepository) : IRequestHandler<UpdateStatusResourceCommand, bool>
    {
        public async Task<bool> Handle(UpdateStatusResourceCommand request, CancellationToken cancellationToken)
        {
            var _StatusResource = await postRepository.ResourceStatus.FindAsync(request.Id);
            if (_StatusResource != null)
            {
                _StatusResource.Name = request.dto.Name;
                _StatusResource.IsVisible = request.dto.IsVisible;
                _StatusResource.Color = request.dto.Color;

                postRepository.ResourceStatus.Update(_StatusResource);
                await postRepository.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
