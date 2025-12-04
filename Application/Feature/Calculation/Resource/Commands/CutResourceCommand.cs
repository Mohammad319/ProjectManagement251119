using Application.Interfaces;
using Application.Services.CalculationItems.Resource;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Calculation.Resource.Commands
{
    public sealed record CutResourceCommand(List<ResourceTaskItemDTO> Items, int TaskId, int sourceCalcId) : IRequest<bool>;

    public class CutResourceCommandHandler(IResourceService resService) : IRequestHandler<CutResourceCommand, bool>
    {
        public async Task<bool> Handle(CutResourceCommand request, CancellationToken cancellationToken)
        {
            return await resService.CutAsync(request.TaskId, request.sourceCalcId, request.Items, cancellationToken);
        }
    }
}
