using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.Project.Queries
{
    public sealed record GetProjectPostQuery(Guid Id, int UserId, int? DepartmentId) : IRequest<PostProjectDTO?>;

    public sealed class GetProjectPostQueryHandler(IProjectService service)
        : IRequestHandler<GetProjectPostQuery, PostProjectDTO?>
    {
        public Task<PostProjectDTO?> Handle(GetProjectPostQuery request, CancellationToken ct)
            => service.GetPostAsync(request.Id, request.UserId, request.DepartmentId, ct);
    }
}
