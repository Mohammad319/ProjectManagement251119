using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.ProjectStatus.Queries
{
    public sealed record GetProjectStatusQuery() : IRequest<List<ProjectStatusEntity>>;

    public sealed class GetProjectStatusQueryHandler(ILookupStatusCommandService<ProjectStatusEntity> service)
        : IRequestHandler<GetProjectStatusQuery, List<ProjectStatusEntity>>
    {
        public Task<List<ProjectStatusEntity>> Handle(GetProjectStatusQuery request, CancellationToken cancellationToken)
            => service.GetAllAsync(cancellationToken);
    }

    public sealed record GetProjectStatusAdminListQuery() : IRequest<IReadOnlyList<LookupAdminListItemDto>>;

    public sealed class GetProjectStatusAdminListQueryHandler(ILookupStatusCommandService<ProjectStatusEntity> service)
        : IRequestHandler<GetProjectStatusAdminListQuery, IReadOnlyList<LookupAdminListItemDto>>
    {
        public Task<IReadOnlyList<LookupAdminListItemDto>> Handle(GetProjectStatusAdminListQuery request, CancellationToken cancellationToken)
            => service.GetAllListAsync(cancellationToken);
    }

    public sealed record GetVisualProjectStatusQuery(int? Id) : IRequest<IEnumerable<ListOrderDTO>>;

    public sealed class GetVisualProjectStatusQueryHandler(ILookupStatusCommandService<ProjectStatusEntity> service)
        : IRequestHandler<GetVisualProjectStatusQuery, IEnumerable<ListOrderDTO>>
    {
        public Task<IEnumerable<ListOrderDTO>> Handle(GetVisualProjectStatusQuery request, CancellationToken cancellationToken)
            => service.GetVisualAsync(request.Id, cancellationToken);
    }
}
