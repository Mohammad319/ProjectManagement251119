using Application.Interfaces;
using Application.Services.CalculationItems.Task;

namespace Application.Feature.Calculation.Task.Commands
{
    public sealed record NewOrderTaskCommand(int Id, double NewOrder) : IRequest<bool>;
    public class NewOrderTaskCommandHandler(ITaskService taskService) : IRequestHandler<NewOrderTaskCommand, bool>
    {
        public async Task<bool> Handle(NewOrderTaskCommand request, CancellationToken ct)
        {
            return await taskService.NewOrderAsync(
                request.Id,
                request.NewOrder,
                ct
            );
        }

    }
}
