using Application.Interfaces;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Organisation.OrganisationType.Queries
{
    public sealed record GetListOrganisationsTypeQuery(int? GroupID, int? CustomerID) : IRequest<IEnumerable<ListDTO>>;
    public class GetListOrganisationsTypeQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetListOrganisationsTypeQuery, IEnumerable<ListDTO>>
    {
        public async Task<IEnumerable<ListDTO>> Handle(GetListOrganisationsTypeQuery query, CancellationToken cancellationToken)
        {
            var list = context.OrganisationType.AsNoTracking();
            if (query.GroupID.HasValue)
                list = list.Where(x => x.IsVisible == true || x.Id == query.GroupID);
            if (query.CustomerID.HasValue)
            {
                list = list.Where(x => x.IsVisible == true || x.Organisations.Any(c => c.Id == query.CustomerID));
            }

            else list = list.Where(x => x.IsVisible == true);
            return await list.Select(x => new ListDTO
            {
                Id = x.Id,
                Name = x.Name,
            }).ToListAsync(cancellationToken);
        }
    }
}
