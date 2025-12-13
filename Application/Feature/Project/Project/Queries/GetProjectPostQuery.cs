using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.Project.Queries
{
    public sealed record GetProjectPostQuery(Guid Id) : IRequest<PostProjectDTO?>;

    public sealed class GetProjectPostQueryHandler(IProjectService service)
        : IRequestHandler<GetProjectPostQuery, PostProjectDTO?>
    {
        public Task<PostProjectDTO?> Handle(GetProjectPostQuery request, CancellationToken ct)
            => service.GetPostAsync(request.Id, ct);
    }
}
