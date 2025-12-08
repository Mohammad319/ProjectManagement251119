using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;
using System;
using System.Linq.Expressions;

namespace Application.Feature.Project.Contract.Queries
{
    public sealed record GetVisualContractQuery(int? ID) : IRequest<IEnumerable<ListOrderDTO>>;
    public class GetVisualContractQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetVisualContractQuery, IEnumerable<ListOrderDTO>>
    {
        public async Task<IEnumerable<ListOrderDTO>> Handle(GetVisualContractQuery query, CancellationToken cancellationToken)
        {
            Expression<Func<ContractEntity, bool>> predicate;
            if (query.ID.HasValue)
                predicate = x => x.IsVisible == true || x.Id == query.ID;
            else
                predicate = x => x.IsVisible == true;

            return await context.Contract.AsNoTracking().Where(predicate).Select(x => new ListOrderDTO
            {
                Id = x.Id,
                Name = x.Name,
                Order = x.SortOrder,
            }).ToListAsync(cancellationToken);
        }
    }
}
