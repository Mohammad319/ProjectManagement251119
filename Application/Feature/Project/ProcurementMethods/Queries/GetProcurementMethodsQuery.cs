using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;

namespace Application.Feature.Project.ProcurementMethods.Queries
{
    public sealed record GetProcurementMethodsQuery() : IRequest<List<ProcurementMethodEntity>>;

    public sealed class GetProcurementMethodsQueryHandler(ILookupStatusCommandService<ProcurementMethodEntity> service)
        : IRequestHandler<GetProcurementMethodsQuery, List<ProcurementMethodEntity>>
    {
        public Task<List<ProcurementMethodEntity>> Handle(GetProcurementMethodsQuery query, CancellationToken cancellationToken)
            => service.GetAllAsync(cancellationToken);
    }

    public sealed record GetProcurementMethodsAdminListQuery() : IRequest<IReadOnlyList<LookupAdminListItemDto>>;

    public sealed class GetProcurementMethodsAdminListQueryHandler(ILookupStatusCommandService<ProcurementMethodEntity> service)
        : IRequestHandler<GetProcurementMethodsAdminListQuery, IReadOnlyList<LookupAdminListItemDto>>
    {
        public Task<IReadOnlyList<LookupAdminListItemDto>> Handle(GetProcurementMethodsAdminListQuery query, CancellationToken cancellationToken)
            => service.GetAllListAsync(cancellationToken);
    }
}
