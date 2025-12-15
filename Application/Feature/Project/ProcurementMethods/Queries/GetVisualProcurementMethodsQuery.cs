using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;
using System;
using System.Linq.Expressions;

namespace Application.Feature.Project.ProcurementMethods.Queries
{
    public sealed record GetVisualProcurementMethodsQuery(int? ID) : IRequest<IEnumerable<ListOrderDTO>>;
    public class GetVisualProcurementMethodsQueryHandler(
        ILookupStatusQueryService<ProcurementMethodEntity> service) : IRequestHandler<GetVisualProcurementMethodsQuery, IEnumerable<ListOrderDTO>>
    {
        public Task<IEnumerable<ListOrderDTO>> Handle(GetVisualProcurementMethodsQuery query, CancellationToken cancellationToken)
            => service.GetVisualAsync(query.ID, cancellationToken);
    }
}
