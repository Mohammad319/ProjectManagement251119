using Application.Interfaces;
using Application.Services.CalculationItems.Task;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Calculation.Task.Commands
{
    public sealed record CopyTaskCommand(
        List<ResourceTaskItemDTO> Items,
        int? TaskParentID,
        int OldCalcId,
        int NewNetCalcId,
        bool IsOH,
        bool DeleteOld = false
    ) : IRequest<bool>;

    public class CopyTaskCommandHandler(ITaskService taskService) : IRequestHandler<CopyTaskCommand, bool>
    {
        public async Task<bool> Handle(CopyTaskCommand request, CancellationToken cancellationToken)
        {
            return await taskService.CopyAsync(
                request.Items,
                request.TaskParentID,
                request.OldCalcId,
                request.NewNetCalcId,
                request.IsOH,
                request.DeleteOld,
                cancellationToken
            );
        }
    }
}