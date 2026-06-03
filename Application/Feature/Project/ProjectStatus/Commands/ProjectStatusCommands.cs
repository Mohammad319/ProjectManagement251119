using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Project.ProjectStatus.Commands
{
    public sealed record CreateProjectStatusCommand(PostTaskStatusDTO Dto) : IRequest<int>;

    public sealed class CreateProjectStatusCommandHandler(
        ILookupStatusCommandService<ProjectStatusEntity> service)
        : IRequestHandler<CreateProjectStatusCommand, int>
    {
        public Task<int> Handle(CreateProjectStatusCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, ct);
    }

    public sealed record UpdateProjectStatusCommand(int Id, PostTaskStatusDTO Dto) : IRequest<bool>;

    public sealed class UpdateProjectStatusCommandHandler(
        ILookupStatusCommandService<ProjectStatusEntity> service)
        : IRequestHandler<UpdateProjectStatusCommand, bool>
    {
        public Task<bool> Handle(UpdateProjectStatusCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, ct);
    }

    public sealed record DeleteProjectStatusCommand(int Id) : IRequest<bool>;

    public sealed class DeleteProjectStatusCommandHandler(
        ILookupStatusCommandService<ProjectStatusEntity> service)
        : IRequestHandler<DeleteProjectStatusCommand, bool>
    {
        public Task<bool> Handle(DeleteProjectStatusCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }

    public sealed record MoveProjectStatusCommand(int Id, bool MoveUp) : IRequest<bool>;

    public sealed class MoveProjectStatusCommandHandler(
        ILookupStatusCommandService<ProjectStatusEntity> service)
        : IRequestHandler<MoveProjectStatusCommand, bool>
    {
        public Task<bool> Handle(MoveProjectStatusCommand request, CancellationToken ct)
            => service.MoveAsync(request.Id, request.MoveUp, ct);
    }

    public sealed record CountProjectsUsingProjectStatusQuery(int Id) : IRequest<int>;

    public sealed class CountProjectsUsingProjectStatusQueryHandler(
        ILookupStatusCommandService<ProjectStatusEntity> service)
        : IRequestHandler<CountProjectsUsingProjectStatusQuery, int>
    {
        public Task<int> Handle(CountProjectsUsingProjectStatusQuery request, CancellationToken ct)
            => service.CountProjectsByStatusAsync(request.Id, ct);
    }
}
