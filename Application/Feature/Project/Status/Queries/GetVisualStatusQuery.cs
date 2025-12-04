using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;
using System;
using System.Linq.Expressions;

namespace Application.Feature.Project.Status.Queries
{
    public sealed record GetVisualStatusQuery(int? ID) : IRequest<IEnumerable<ListOrderDTO>>;
    public class GetVisualStatusQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetVisualStatusQuery, IEnumerable<ListOrderDTO>>
    {
        public async Task<IEnumerable<ListOrderDTO>> Handle(GetVisualStatusQuery query, CancellationToken cancellationToken)
        {
            Expression<Func<StatusEntity, bool>> predicate;
            if (query.ID.HasValue)
                predicate = x => x.IsVisible == true || x.Id == query.ID;
            else
                predicate = x => x.IsVisible == true;
            return await context.CalculationStatus.Where(predicate).AsNoTracking().Select(x => new ListOrderDTO
            {
                Id = x.Id,
                Name = x.Name,
                Order = x.Order,
            }).ToListAsync(cancellationToken);
        }
    }
}
