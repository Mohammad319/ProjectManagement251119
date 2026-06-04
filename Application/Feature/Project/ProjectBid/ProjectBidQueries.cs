using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.ProjectBid
{
    public sealed record GetProjectBidsQuery(
        Guid ProjectId,
        int? DepartmentId
    ) : IRequest<List<ProjectBidListDTO>>;

    public sealed class GetProjectBidsQueryHandler(IProjectBidService service)
        : IRequestHandler<GetProjectBidsQuery, List<ProjectBidListDTO>>
    {
        public Task<List<ProjectBidListDTO>> Handle(GetProjectBidsQuery request, CancellationToken cancellationToken)
            => service.GetByProjectAsync(request.ProjectId, request.DepartmentId, cancellationToken);
    }
}
