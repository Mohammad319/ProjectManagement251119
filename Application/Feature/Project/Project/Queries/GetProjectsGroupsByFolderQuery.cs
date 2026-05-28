using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;
using System;

namespace Application.Feature.Project.Project.Queries
{
    public sealed record GetProjectsGroupsByFolderQuery(Guid FolderId, bool IncludeArchived, int UserId, int? DepartmentId)
    : IRequest<IEnumerable<ListProjectDTO>>;

    public sealed class GetProjectsGroupsByFolderQueryHandler(IProjectService service)
        : IRequestHandler<GetProjectsGroupsByFolderQuery, IEnumerable<ListProjectDTO>>
    {
        public Task<IEnumerable<ListProjectDTO>> Handle(GetProjectsGroupsByFolderQuery request, CancellationToken ct)
            => service.GetByFolderAsync(request.FolderId, request.IncludeArchived, request.UserId, request.DepartmentId, ct);
    }
}
