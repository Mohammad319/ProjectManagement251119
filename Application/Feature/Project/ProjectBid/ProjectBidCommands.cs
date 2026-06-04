using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.ProjectBid
{
    // ─── Create ───────────────────────────────────────────────────────────────
    public sealed record CreateProjectBidCommand(
        Guid ProjectId,
        ProjectBidPostDTO Dto,
        int? DepartmentId
    ) : IRequest<int>;

    public sealed class CreateProjectBidCommandHandler(IProjectBidService service)
        : IRequestHandler<CreateProjectBidCommand, int>
    {
        public Task<int> Handle(CreateProjectBidCommand request, CancellationToken cancellationToken)
            => service.CreateAsync(request.ProjectId, request.Dto, request.DepartmentId, cancellationToken);
    }

    // ─── Update ───────────────────────────────────────────────────────────────
    public sealed record UpdateProjectBidCommand(
        int Id,
        Guid ProjectId,
        ProjectBidPostDTO Dto,
        int? DepartmentId
    ) : IRequest<bool>;

    public sealed class UpdateProjectBidCommandHandler(IProjectBidService service)
        : IRequestHandler<UpdateProjectBidCommand, bool>
    {
        public Task<bool> Handle(UpdateProjectBidCommand request, CancellationToken cancellationToken)
            => service.UpdateAsync(request.Id, request.ProjectId, request.Dto, request.DepartmentId, cancellationToken);
    }

    // ─── Delete ───────────────────────────────────────────────────────────────
    public sealed record DeleteProjectBidCommand(
        int Id,
        Guid ProjectId,
        int? DepartmentId
    ) : IRequest<bool>;

    public sealed class DeleteProjectBidCommandHandler(IProjectBidService service)
        : IRequestHandler<DeleteProjectBidCommand, bool>
    {
        public Task<bool> Handle(DeleteProjectBidCommand request, CancellationToken cancellationToken)
            => service.DeleteAsync(request.Id, request.ProjectId, request.DepartmentId, cancellationToken);
    }
}
