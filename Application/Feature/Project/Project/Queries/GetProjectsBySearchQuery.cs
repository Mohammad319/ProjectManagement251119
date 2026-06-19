using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.Project.Queries
{
    public sealed record GetProjectsBySearchQuery(ProjectFilter Filter, int UserId, int? DepartmentId, bool IsViewer = false)
        : IRequest<IEnumerable<SearchProjectDTO>>;

    public sealed class GetProjectsBySearchQueryHandler(IProjectService service)
        : IRequestHandler<GetProjectsBySearchQuery, IEnumerable<SearchProjectDTO>>
    {
        public Task<IEnumerable<SearchProjectDTO>> Handle(GetProjectsBySearchQuery request, CancellationToken ct)
            => service.SearchAsync(request.Filter, request.UserId, request.DepartmentId, ct, request.IsViewer);
    }
}
