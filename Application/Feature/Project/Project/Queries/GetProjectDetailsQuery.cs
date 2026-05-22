using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;
using System;

namespace Application.Feature.Project.Project.Queries
{
    public sealed record GetProjectDetailsQuery(Guid Id, int UserId, int? DepartmentId) : IRequest<ProjectDetailsDTO?>;

    public sealed class GetProjectDetailsQueryHandler(IProjectService service)
        : IRequestHandler<GetProjectDetailsQuery, ProjectDetailsDTO?>
    {
        public Task<ProjectDetailsDTO?> Handle(GetProjectDetailsQuery request, CancellationToken ct)
            => service.GetDetailsAsync(request.Id, request.UserId, request.DepartmentId, ct);
    }
}
