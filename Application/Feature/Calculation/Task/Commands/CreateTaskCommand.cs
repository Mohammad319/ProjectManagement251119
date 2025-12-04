using Application.Interfaces;
using Application.Services.CalculationItems.Task;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Task.Commands
{
    public sealed record CreateTaskCommand(
        List<TaskPostDTO> Tasks,
        int NewNetCalcId
        ) : IRequest<bool>;

    public class CreateTaskCommandHandler(ITaskService taskService) : IRequestHandler<CreateTaskCommand, bool>
    {
        public async Task<bool> Handle(CreateTaskCommand request, CancellationToken ct)
        {
            return await taskService.CreateAsync(
                request.Tasks,
                request.NewNetCalcId,
                ct);
        }
    }
}
