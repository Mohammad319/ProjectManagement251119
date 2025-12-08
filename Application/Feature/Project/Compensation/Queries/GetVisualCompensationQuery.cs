using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;
using System;
using System.Linq.Expressions;

namespace Application.Feature.Project.Compensation.Queries
{
    public sealed record GetVisualCompensationQuery(int? ID) : IRequest<IEnumerable<ListOrderDTO>>;
    public class GetVisualCompensationQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetVisualCompensationQuery, IEnumerable<ListOrderDTO>>
    {
        public async Task<IEnumerable<ListOrderDTO>> Handle(GetVisualCompensationQuery query, CancellationToken cancellationToken)
        {
            Expression<Func<CompensationEntity, bool>> predicate;
            if (query.ID.HasValue) predicate = x => x.IsVisible == true || x.Id == query.ID;
            else predicate = x => x.IsVisible == true;

            return await context.Compensation.Where(predicate).AsNoTracking().Select(x => new ListOrderDTO
            {
                Id = x.Id,
                Name = x.Name,
                Order = x.SortOrder
            }).ToListAsync(cancellationToken);
        }
    }
}
