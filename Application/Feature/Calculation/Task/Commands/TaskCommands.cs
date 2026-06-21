using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Calculation.Task.Commands
{
    public sealed record CreateTaskCommand(List<TaskPostDTO> Tasks, int NewNetCalcId, int UserId, int? DepartmentId) : IRequest<List<TaskListDTO>>;

    public class CreateTaskCommandHandler(ITaskService taskService) : IRequestHandler<CreateTaskCommand, List<TaskListDTO>>
    {
        public async Task<List<TaskListDTO>> Handle(CreateTaskCommand request, CancellationToken ct)
        {
            return await taskService.CreateAsync(request.Tasks, request.NewNetCalcId, request.UserId, request.DepartmentId, ct);
        }
    }

    public sealed record CopyTaskCommand(List<ResourceTaskItemDTO> Items, int? TaskParentID, int OldCalcId, int NewNetCalcId, bool IsOH,
    int UserId, bool DeleteOld = false, int? DepartmentId = null) : IRequest<bool>;

    public class CopyTaskCommandHandler(ITaskService taskService) : IRequestHandler<CopyTaskCommand, bool>
    {
        public async Task<bool> Handle(CopyTaskCommand request, CancellationToken cancellationToken)
        {
            return await taskService.CopyAsync(request.Items, request.TaskParentID, request.OldCalcId, request.NewNetCalcId, request.IsOH,
                request.UserId, request.DeleteOld, request.DepartmentId, cancellationToken);
        }
    }
    public class CutTaskCommand : IRequest<bool>
    {
        public int OldCalcId { get; set; }
        public int NewNetCalcId { get; set; }
        public int? TaskParentID { get; set; }
        public List<ResourceTaskItemDTO> Items { get; set; } = [];
        public int Order { get; set; } = 100;
        public bool IsOH { get; set; } = false;
        public int UserId { get; set; }
        public int? DepartmentId { get; set; }

        public class CutTaskCommandHandler(ITaskService taskService) : IRequestHandler<CutTaskCommand, bool>
        {
            public async Task<bool> Handle(CutTaskCommand request, CancellationToken cancellationToken)
            {
                var result = await taskService.CutAsync(request.OldCalcId, request.NewNetCalcId, request.TaskParentID, request.Items, request.UserId, request.Order,
                     request.IsOH, request.DepartmentId, cancellationToken);
                return result;
            }
        }
    }
    public sealed record DeleteTaskCommand(IEnumerable<int> Items, int CalcID, int UserId, int? DepartmentId) : IRequest<bool>;
    public class DeleteTaskCommandHandler(ITaskService taskService) : IRequestHandler<DeleteTaskCommand, bool>
    {
        public async Task<bool> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
        {
            return await taskService.DeleteAsync(request.Items, request.CalcID, request.UserId, request.DepartmentId, cancellationToken
            );
        }
    }

    public sealed record NewOrderTaskCommand(int Id, int NewOrder, int UserId, int? DepartmentId) : IRequest<bool>;
    public class NewOrderTaskCommandHandler(ITaskService taskService) : IRequestHandler<NewOrderTaskCommand, bool>
    {
        public async Task<bool> Handle(NewOrderTaskCommand request, CancellationToken ct) =>
             await taskService.NewOrderAsync(request.Id, request.NewOrder, request.UserId, request.DepartmentId, ct);
    }
    public sealed record UpdateTaskCommand(int Id, TaskPostDTO Dto, int UserId, int? DepartmentId) : IRequest<bool>;
    public class UpdateTaskCommandHandler(ITaskService taskService) : IRequestHandler<UpdateTaskCommand, bool>
    {
        public async Task<bool> Handle(UpdateTaskCommand request, CancellationToken ct)
        {
            return await taskService.UpdateAsync(request.Id, request.Dto, request.UserId, request.DepartmentId, ct);
        }
    }
}
