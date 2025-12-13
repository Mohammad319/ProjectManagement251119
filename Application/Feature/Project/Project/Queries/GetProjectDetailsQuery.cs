using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;
using System;

namespace Application.Feature.Project.Project.Queries
{
    public sealed record GetProjectDetailsQuery(Guid Id) : IRequest<ProjectDetailsDTO?>;

    public sealed class GetProjectDetailsQueryHandler(IProjectService service)
        : IRequestHandler<GetProjectDetailsQuery, ProjectDetailsDTO?>
    {
        public Task<ProjectDetailsDTO?> Handle(GetProjectDetailsQuery request, CancellationToken ct)
            => service.GetDetailsAsync(request.Id, ct);
    }
}
