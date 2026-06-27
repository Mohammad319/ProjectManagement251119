using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.ProjectBid
{
    public sealed record GetProjectBidsQuery(
        Guid ProjectId,
        int UserId,
        int? DepartmentId,
        bool IsViewer
    ) : IRequest<ProjectBidsViewDTO>;

    public sealed class GetProjectBidsQueryHandler(IProjectBidService service)
        : IRequestHandler<GetProjectBidsQuery, ProjectBidsViewDTO>
    {
        public Task<ProjectBidsViewDTO> Handle(GetProjectBidsQuery request, CancellationToken cancellationToken)
            => service.GetViewAsync(request.ProjectId, request.UserId, request.DepartmentId, request.IsViewer, cancellationToken);
    }

    // ─── Anbudsjämförelse (bulk över flera projekt) ───────────────────────────
    public sealed record GetProjectBidComparisonQuery(
        IReadOnlyList<Guid> ProjectIds,
        int UserId,
        int? DepartmentId,
        bool IsViewer
    ) : IRequest<List<ProjectBidComparisonRowDTO>>;

    public sealed class GetProjectBidComparisonQueryHandler(IProjectBidService service)
        : IRequestHandler<GetProjectBidComparisonQuery, List<ProjectBidComparisonRowDTO>>
    {
        public Task<List<ProjectBidComparisonRowDTO>> Handle(GetProjectBidComparisonQuery request, CancellationToken cancellationToken)
            => service.GetComparisonAsync(request.ProjectIds, request.UserId, request.DepartmentId, request.IsViewer, cancellationToken);
    }
}
