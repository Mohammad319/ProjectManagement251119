using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;
using System;

namespace Application.Feature.Project.Project.Queries
{
    public sealed record GetProjectsOtherGroupByFolderQuery(Guid FolderId, int UserId, int? DepartmentId)
        : IRequest<IEnumerable<ListProjectDTO>>;

    public sealed class GetProjectsOtherGroupByFolderQueryHandler(IProjectService service)
        : IRequestHandler<GetProjectsOtherGroupByFolderQuery, IEnumerable<ListProjectDTO>>
    {
        public Task<IEnumerable<ListProjectDTO>> Handle(GetProjectsOtherGroupByFolderQuery request, CancellationToken ct)
            => service.GetOtherGroupByFolderAsync(request.FolderId, request.UserId, request.DepartmentId, ct);
    }
}
