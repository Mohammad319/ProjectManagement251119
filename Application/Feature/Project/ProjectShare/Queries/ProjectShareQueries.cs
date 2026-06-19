using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.ProjectShare.Queries
{
    public sealed record GetProjectSharesQuery(Guid ProjectId, int? DepartmentId, int UserId)
        : IRequest<IReadOnlyList<ProjectShareListItemDTO>>;

    public sealed class GetProjectSharesQueryHandler(IProjectShareService service)
        : IRequestHandler<GetProjectSharesQuery, IReadOnlyList<ProjectShareListItemDTO>>
    {
        public Task<IReadOnlyList<ProjectShareListItemDTO>> Handle(GetProjectSharesQuery request, CancellationToken ct)
            => service.GetByProjectAsync(request.ProjectId, request.DepartmentId, request.UserId, ct);
    }
}
