using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.Project.Commands
{
    public sealed record CreateProjectCommand(PostProjectDTO Dto, int UserId, int? DepartmentId) : IRequest<Guid>;

    public sealed class CreateProjectCommandHandler(IProjectService service)
        : IRequestHandler<CreateProjectCommand, Guid>
    {
        public Task<Guid> Handle(CreateProjectCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, request.UserId, request.DepartmentId, ct);
    }

    public sealed record EditProjectCommand(Guid Id, PostProjectDTO Dto, int UserId, int? DepartmentId) : IRequest<bool>;

    public sealed class EditProjectCommandHandler(IProjectService service)
        : IRequestHandler<EditProjectCommand, bool>
    {
        public Task<bool> Handle(EditProjectCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, request.UserId, request.DepartmentId, ct);
    }

    public sealed record MoveProjectCommand(Guid Id, Guid TargetFolderId, int UserId, int? DepartmentId, bool AllowCrossDepartment) : IRequest<bool>;

    public sealed class MoveProjectCommandHandler(IProjectService service)
        : IRequestHandler<MoveProjectCommand, bool>
    {
        public Task<bool> Handle(MoveProjectCommand request, CancellationToken ct)
            => service.MoveAsync(request.Id, request.TargetFolderId, request.UserId, request.DepartmentId, request.AllowCrossDepartment, ct);
    }

    public sealed record CopyProjectCommand(Guid Id, Guid TargetFolderId, bool IncludeCalculations, int UserId, int? DepartmentId, bool AllowCrossDepartment) : IRequest<Guid>;

    public sealed class CopyProjectCommandHandler(IProjectService service)
        : IRequestHandler<CopyProjectCommand, Guid>
    {
        public Task<Guid> Handle(CopyProjectCommand request, CancellationToken ct)
            => service.CopyAsync(request.Id, request.TargetFolderId, request.IncludeCalculations, request.UserId, request.DepartmentId, request.AllowCrossDepartment, ct);
    }

    public sealed record DeleteProjectCommand(Guid Id, int UserId, int? DepartmentId) : IRequest<bool>;

    public sealed class DeleteProjectCommandHandler(IProjectService service)
        : IRequestHandler<DeleteProjectCommand, bool>
    {
        public Task<bool> Handle(DeleteProjectCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, request.UserId, request.DepartmentId, ct);
    }

    public sealed record NewOrderProjectCommand(Guid Id, int NewOrder, int? DepartmentId) : IRequest<bool>;

    public sealed class NewOrderProjectCommandHandler(IProjectService service)
        : IRequestHandler<NewOrderProjectCommand, bool>
    {
        public Task<bool> Handle(NewOrderProjectCommand request, CancellationToken ct)
            => service.UpdateOrderAsync(request.Id, request.NewOrder, request.DepartmentId, ct);
    }
}
