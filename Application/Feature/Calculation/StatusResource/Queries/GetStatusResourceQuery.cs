using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.StatusResource.Queries
{
    public sealed record GetResourceStatusQuery()
        : IRequest<List<StatusResourcesEntity>>;

    public sealed class GetResourceStatusQueryHandler(ILookupStatusQueryService<StatusResourcesEntity> service)
                : IRequestHandler<GetResourceStatusQuery, List<StatusResourcesEntity>>
    {
        public Task<List<StatusResourcesEntity>> Handle(GetResourceStatusQuery request, CancellationToken cancellationToken)
            => service.GetAllAsync(cancellationToken);
    }

    public sealed record GetVisualResourceStatusQuery(int? Id)
        : IRequest<IEnumerable<ListOrderDTO>>;

    public sealed class GetVisualResourceStatusQueryHandler(ILookupStatusQueryService<StatusResourcesEntity> service)
                : IRequestHandler<GetVisualResourceStatusQuery, IEnumerable<ListOrderDTO>>
    {
        public Task<IEnumerable<ListOrderDTO>> Handle(GetVisualResourceStatusQuery request, CancellationToken cancellationToken)
            => service.GetVisualAsync(request.Id, cancellationToken);
    }
}
