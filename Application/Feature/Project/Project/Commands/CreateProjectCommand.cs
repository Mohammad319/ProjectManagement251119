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
