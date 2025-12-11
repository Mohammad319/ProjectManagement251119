using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Resource.Commands
{
    public sealed record UpdateResourceCommand(int Id, ResourcePostDTO Dto) : IRequest<bool>;

    public class UpdateResourceCommandHandler(IResourceService resService) : IRequestHandler<UpdateResourceCommand, bool>
    {
        public async Task<bool> Handle(UpdateResourceCommand request, CancellationToken cancellationToken)
        {
            return await resService.UpdateAsync(request.Id, request.Dto, cancellationToken);
        }

    }
}
