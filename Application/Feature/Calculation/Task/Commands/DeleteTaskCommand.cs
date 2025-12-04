using Application.Interfaces;
using Application.Services.CalculationItems.Task;

namespace Application.Feature.Calculation.Task.Commands
{
    public sealed record DeleteTaskCommand(
        IEnumerable<int> Items,
        int CalcID
        ) : IRequest<bool>;

    public class DeleteTaskCommandHandler(ITaskService taskService) : IRequestHandler<DeleteTaskCommand, bool>
    {
        public async Task<bool> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
        {
            return await taskService.DeleteAsync(
                request.Items,
                request.CalcID,
                cancellationToken
            );
        }
    }
}
