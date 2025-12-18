using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;

namespace Application.Feature.Project.ProcurementMethods.Queries
{
    public sealed record GetProcurementMethodsQuery() : IRequest<List<ProcurementMethodEntity>>;
    public class GetProcurementMethodsQueryHandler(
        ILookupStatusCommandService<ProcurementMethodEntity> service) : IRequestHandler<GetProcurementMethodsQuery, List<ProcurementMethodEntity>>
    {
        public Task<List<ProcurementMethodEntity>> Handle(GetProcurementMethodsQuery query, CancellationToken cancellationToken)
            => service.GetAllAsync(cancellationToken);
    }
}
