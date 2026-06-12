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

    // ─── Price columns ────────────────────────────────────────────────────────
    public sealed record CreateProjectBidPriceColumnCommand(
        Guid ProjectId,
        ProjectBidPriceColumnPostDTO Dto,
        int? DepartmentId
    ) : IRequest<int>;

    public sealed class CreateProjectBidPriceColumnCommandHandler(IProjectBidService service)
        : IRequestHandler<CreateProjectBidPriceColumnCommand, int>
    {
        public Task<int> Handle(CreateProjectBidPriceColumnCommand request, CancellationToken cancellationToken)
            => service.CreatePriceColumnAsync(request.ProjectId, request.Dto, request.DepartmentId, cancellationToken);
    }

    public sealed record RenameProjectBidPriceColumnCommand(
        int Id,
        Guid ProjectId,
        ProjectBidPriceColumnPostDTO Dto,
        int? DepartmentId
    ) : IRequest<bool>;

    public sealed class RenameProjectBidPriceColumnCommandHandler(IProjectBidService service)
        : IRequestHandler<RenameProjectBidPriceColumnCommand, bool>
    {
        public Task<bool> Handle(RenameProjectBidPriceColumnCommand request, CancellationToken cancellationToken)
            => service.RenamePriceColumnAsync(request.Id, request.ProjectId, request.Dto, request.DepartmentId, cancellationToken);
    }

    public sealed record DeleteProjectBidPriceColumnCommand(
        int Id,
        Guid ProjectId,
        int? DepartmentId
    ) : IRequest<bool>;

    public sealed class DeleteProjectBidPriceColumnCommandHandler(IProjectBidService service)
        : IRequestHandler<DeleteProjectBidPriceColumnCommand, bool>
    {
        public Task<bool> Handle(DeleteProjectBidPriceColumnCommand request, CancellationToken cancellationToken)
            => service.DeletePriceColumnAsync(request.Id, request.ProjectId, request.DepartmentId, cancellationToken);
    }

    public sealed record MoveProjectBidPriceColumnCommand(
        int Id,
        Guid ProjectId,
        int Direction,
        int? DepartmentId
    ) : IRequest<bool>;

    public sealed class MoveProjectBidPriceColumnCommandHandler(IProjectBidService service)
        : IRequestHandler<MoveProjectBidPriceColumnCommand, bool>
    {
        public Task<bool> Handle(MoveProjectBidPriceColumnCommand request, CancellationToken cancellationToken)
            => service.MovePriceColumnAsync(request.Id, request.ProjectId, request.Direction, request.DepartmentId, cancellationToken);
    }
}
