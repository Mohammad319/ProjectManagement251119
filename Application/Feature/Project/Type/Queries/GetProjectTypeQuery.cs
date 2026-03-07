using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.Type.Queries
{
    public sealed record GetTypeQuery() : IRequest<List<TypeEntity>>;

    public sealed class GetTypeQueryHandler(ILookupStatusCommandService<TypeEntity> service)
        : IRequestHandler<GetTypeQuery, List<TypeEntity>>
    {
        public Task<List<TypeEntity>> Handle(GetTypeQuery request, CancellationToken cancellationToken)
            => service.GetAllAsync(cancellationToken);
    }

    public sealed record GetTypeAdminListQuery() : IRequest<IReadOnlyList<LookupAdminListItemDto>>;

    public sealed class GetTypeAdminListQueryHandler(ILookupStatusCommandService<TypeEntity> service)
        : IRequestHandler<GetTypeAdminListQuery, IReadOnlyList<LookupAdminListItemDto>>
    {
        public Task<IReadOnlyList<LookupAdminListItemDto>> Handle(GetTypeAdminListQuery request, CancellationToken cancellationToken)
            => service.GetAllListAsync(cancellationToken);
    }

    public sealed record GetVisualTypeQuery(int? Id) : IRequest<IEnumerable<ListOrderDTO>>;

    public sealed class GetVisualTypeQueryHandler(ILookupStatusCommandService<TypeEntity> service)
        : IRequestHandler<GetVisualTypeQuery, IEnumerable<ListOrderDTO>>
    {
        public Task<IEnumerable<ListOrderDTO>> Handle(GetVisualTypeQuery request, CancellationToken cancellationToken)
            => service.GetVisualAsync(request.Id, cancellationToken);
    }
}
