using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Calculation.StatusResource.Queries
{
    public sealed record GetResourceStatusQuery() : IRequest<List<StatusResourcesEntity>>;

    public sealed class GetResourceStatusQueryHandler(ILookupStatusCommandService<StatusResourcesEntity> service)
        : IRequestHandler<GetResourceStatusQuery, List<StatusResourcesEntity>>
    {
        public Task<List<StatusResourcesEntity>> Handle(GetResourceStatusQuery request, CancellationToken cancellationToken)
            => service.GetAllAsync(cancellationToken);
    }

    public sealed record GetResourceStatusAdminListQuery() : IRequest<IReadOnlyList<LookupAdminListItemDto>>;

    public sealed class GetResourceStatusAdminListQueryHandler(ILookupStatusCommandService<StatusResourcesEntity> service)
        : IRequestHandler<GetResourceStatusAdminListQuery, IReadOnlyList<LookupAdminListItemDto>>
    {
        public Task<IReadOnlyList<LookupAdminListItemDto>> Handle(GetResourceStatusAdminListQuery request, CancellationToken cancellationToken)
            => service.GetAllListAsync(cancellationToken);
    }

    public sealed record GetVisualResourceStatusQuery(int? Id) : IRequest<IEnumerable<ListOrderDTO>>;

    public sealed class GetVisualResourceStatusQueryHandler(ILookupStatusCommandService<StatusResourcesEntity> service)
        : IRequestHandler<GetVisualResourceStatusQuery, IEnumerable<ListOrderDTO>>
    {
        public Task<IEnumerable<ListOrderDTO>> Handle(GetVisualResourceStatusQuery request, CancellationToken cancellationToken)
            => service.GetVisualAsync(request.Id, cancellationToken);
    }
}
