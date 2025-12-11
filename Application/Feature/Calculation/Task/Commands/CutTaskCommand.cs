using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Calculation.Task.Commands
{
    public class CutTaskCommand : IRequest<bool>
    {
        public int OldCalcId { get; set; }
        public int NewNetCalcId { get; set; }
        public int? TaskParentID { get; set; }
        public List<ResourceTaskItemDTO> Items { get; set; } = [];
        public double Order { get; set; } = 100;
        public bool IsOH { get; set; } = false;

        public class CutTaskCommandHandler : IRequestHandler<CutTaskCommand, bool>
        {
            private readonly ITaskService _taskService;

            public CutTaskCommandHandler(ITaskService taskService)
            {
                _taskService = taskService;
            }

            public async Task<bool> Handle(CutTaskCommand request, CancellationToken cancellationToken)
            {
                // هنا نرمي كل الشغل الثقيل على TaskService.CutAsync
                var result = await _taskService.CutAsync(
                    sourceCalcId: request.OldCalcId,
                    targetCalcId: request.NewNetCalcId,
                    parentTaskId: request.TaskParentID,
                    items: request.Items,
                    order: request.Order,
                    isOH: request.IsOH,
                    cancellationToken: cancellationToken);

                return result;
            }
        }
    }
}
