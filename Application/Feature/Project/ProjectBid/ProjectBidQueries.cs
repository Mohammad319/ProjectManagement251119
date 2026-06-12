using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.ProjectBid
{
    public sealed record GetProjectBidsQuery(
        Guid ProjectId,
        int? DepartmentId
    ) : IRequest<ProjectBidsViewDTO>;

    public sealed class GetProjectBidsQueryHandler(IProjectBidService service)
        : IRequestHandler<GetProjectBidsQuery, ProjectBidsViewDTO>
    {
        public Task<ProjectBidsViewDTO> Handle(GetProjectBidsQuery request, CancellationToken cancellationToken)
            => service.GetViewAsync(request.ProjectId, request.DepartmentId, cancellationToken);
    }
}
