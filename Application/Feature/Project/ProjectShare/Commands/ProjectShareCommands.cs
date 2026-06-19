using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.ProjectShare.Commands
{
    public sealed record UpsertProjectShareCommand(Guid ProjectId, ProjectShareUpsertDTO Dto, int UserId, int? DepartmentId) : IRequest<int>;

    public sealed class UpsertProjectShareCommandHandler(IProjectShareService service)
        : IRequestHandler<UpsertProjectShareCommand, int>
    {
        public Task<int> Handle(UpsertProjectShareCommand request, CancellationToken ct)
            => service.UpsertAsync(request.ProjectId, request.Dto, request.UserId, request.DepartmentId, ct);
    }

    public sealed record DeleteProjectShareCommand(int Id, int UserId, int? DepartmentId) : IRequest<bool>;

    public sealed class DeleteProjectShareCommandHandler(IProjectShareService service)
        : IRequestHandler<DeleteProjectShareCommand, bool>
    {
        public Task<bool> Handle(DeleteProjectShareCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, request.UserId, request.DepartmentId, ct);
    }
}
