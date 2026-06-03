using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.TaskStatus.Commands
{
    // CREATE
    public sealed record CreateTaskStatusCommand(PostTaskStatusDTO Dto) : IRequest<int>;

    public sealed class CreateTaskStatusCommandHandler(ILookupStatusCommandService<TaskStatusEntity> service)
                : IRequestHandler<CreateTaskStatusCommand, int>
    {
        public Task<int> Handle(CreateTaskStatusCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, ct);
    }

    // UPDATE
    public sealed record UpdateTaskStatusCommand(int Id, PostTaskStatusDTO Dto) : IRequest<bool>;

    public sealed class UpdateTaskStatusCommandHandler(ILookupStatusCommandService<TaskStatusEntity> service)
                : IRequestHandler<UpdateTaskStatusCommand, bool>
    {
        public Task<bool> Handle(UpdateTaskStatusCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, ct);
    }

    // DELETE
    public sealed record DeleteTaskStatusCommand(int Id) : IRequest<bool>;

    public sealed class DeleteTaskStatusCommandHandler(ILookupStatusCommandService<TaskStatusEntity> service)
                : IRequestHandler<DeleteTaskStatusCommand, bool>
    {
        public Task<bool> Handle(DeleteTaskStatusCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }

    public sealed record MoveTaskStatusCommand(int Id, bool MoveUp) : IRequest<bool>;

    public sealed class MoveTaskStatusCommandHandler(ILookupStatusCommandService<TaskStatusEntity> service)
        : IRequestHandler<MoveTaskStatusCommand, bool>
    {
        public Task<bool> Handle(MoveTaskStatusCommand request, CancellationToken ct)
            => service.MoveAsync(request.Id, request.MoveUp, ct);
    }
}
