using Application.Interfaces;
using Application.Services.CalculationItems.Task;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Task.Commands
{
    public sealed record UpdateTaskCommand(int Id, TaskPostDTO Dto) : IRequest<bool>;
    public class UpdateTaskCommandHandler(ITaskService taskService) : IRequestHandler<UpdateTaskCommand, bool>
    {
        public async Task<bool> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
        {
            return await taskService.UpdateAsync(
                request.Id,
                request.Dto,
                cancellationToken
            );
        }
    }
}
