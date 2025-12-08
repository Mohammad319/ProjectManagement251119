using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;
using System;
using System.Linq.Expressions;

namespace Application.Feature.Project.ProcurementMethods.Queries
{
    public sealed record GetVisualProcurementMethodsQuery(int? ID) : IRequest<IEnumerable<ListOrderDTO>>;
    public class GetVisualProcurementMethodsQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetVisualProcurementMethodsQuery, IEnumerable<ListOrderDTO>>
    {
        public async Task<IEnumerable<ListOrderDTO>> Handle(GetVisualProcurementMethodsQuery query, CancellationToken cancellationToken)
        {
            Expression<Func<ProcurementMethodEntity, bool>> predicate;
            if (query.ID.HasValue)
                predicate = x => x.IsVisible == true || x.Id == query.ID;
            else
                predicate = x => x.IsVisible == true;
            return await context.ProcurementMethod.Where(predicate).AsNoTracking()
                .Select(x => new ListOrderDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Order = x.SortOrder,
                }).ToListAsync(cancellationToken);
        }
    }
}
