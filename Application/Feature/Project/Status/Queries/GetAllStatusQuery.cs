using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.Status.Queries
{
    public sealed record GetStatusQuery() : IRequest<List<StatusEntity>>;

    public sealed class GetStatusQueryHandler(ILookupStatusCommandService<StatusEntity> service)
        : IRequestHandler<GetStatusQuery, List<StatusEntity>>
    {
        public Task<List<StatusEntity>> Handle(GetStatusQuery request, CancellationToken cancellationToken)
            => service.GetAllAsync(cancellationToken);
    }

    public sealed record GetStatusAdminListQuery() : IRequest<IReadOnlyList<LookupAdminListItemDto>>;

    public sealed class GetStatusAdminListQueryHandler(ILookupStatusCommandService<StatusEntity> service)
        : IRequestHandler<GetStatusAdminListQuery, IReadOnlyList<LookupAdminListItemDto>>
    {
        public Task<IReadOnlyList<LookupAdminListItemDto>> Handle(GetStatusAdminListQuery request, CancellationToken cancellationToken)
            => service.GetAllListAsync(cancellationToken);
    }

    public sealed record GetVisualStatusQuery(int? Id) : IRequest<IEnumerable<ListOrderDTO>>;

    public sealed class GetVisualStatusQueryHandler(ILookupStatusCommandService<StatusEntity> service)
        : IRequestHandler<GetVisualStatusQuery, IEnumerable<ListOrderDTO>>
    {
        public Task<IEnumerable<ListOrderDTO>> Handle(GetVisualStatusQuery request, CancellationToken cancellationToken)
            => service.GetVisualAsync(request.Id, cancellationToken);
    }
}
