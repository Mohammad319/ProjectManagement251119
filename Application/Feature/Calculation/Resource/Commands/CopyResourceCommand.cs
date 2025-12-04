using Application.Extention;
using Application.Interfaces;
using Application.Services.CalculationItems.Resource;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Project;
using System;

namespace Application.Feature.Calculation.Resource.Commands
{
    public sealed record CopyResourceCommand(List<ResourceTaskItemDTO> Items, int parentTaskId, int sourceCalcId) : IRequest<bool>;
    public class CopyResourceCommandHandler(IResourceService resService) : IRequestHandler<CopyResourceCommand, bool>
    {
        public async Task<bool> Handle(CopyResourceCommand request, CancellationToken cancellationToken)
        {
            return await resService.CopyAsync(request.Items, request.parentTaskId, request.sourceCalcId, cancellationToken);
        }
    }
}
