using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Resource.Commands
{
    public sealed record CreateResourceCommand(List<ResourcePostDTO> Items, int parentTaskId) : IRequest<bool>;

    public class CreateResourceCommandHandler(IResourceService resService) : IRequestHandler<CreateResourceCommand, bool>
    {
        public async Task<bool> Handle(CreateResourceCommand request, CancellationToken cancellationToken)
        {
            return await resService.CreateAsync(request.Items, request.parentTaskId, cancellationToken);
        }
    }
}
