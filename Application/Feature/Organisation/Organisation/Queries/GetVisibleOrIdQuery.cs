using Application.Interfaces;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.DTO.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Application.Feature.Organisation.Organisation.Queries
{

    public sealed record GetVisibleOrIdQuery(int orgid) : IRequest<IEnumerable<ListDTO>>;
    public class GetVisibleOrIdQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetVisibleOrIdQuery, IEnumerable<ListDTO>>
    {
        public async Task<IEnumerable<ListDTO>> Handle(GetVisibleOrIdQuery query, CancellationToken cancellationToken)
        {
            return await context.Organisation.Where(x => x.IsVisible || (query.orgid > 0 && x.Id == query.orgid)).OrderByDescending(x => x).AsNoTracking()
                .AsNoTracking().Select(x => new ListDTO()
                {
                    Name = x.Name,
                    Id = x.Id,
                }).ToListAsync(cancellationToken: cancellationToken);
        }
    }

}
