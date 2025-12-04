using Application.Interfaces;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Feature.Organisation.Organisation.Queries
{
    public sealed record GetOrganisationsQuery(int GroupId, bool IsVisible) : IRequest<List<ShortListOrganisationDTO>>;
    public class GetOrganisationsQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetOrganisationsQuery, List<ShortListOrganisationDTO>>
    {
        public async Task<List<ShortListOrganisationDTO>> Handle(GetOrganisationsQuery query, CancellationToken cancellationToken)
        {
            return await context.Organisation.AsNoTracking().OrderByDescending(x => x)
                .Where(x => x.IsVisible == query.IsVisible && x.CategoryId == query.GroupId)
                .Select(x => new ShortListOrganisationDTO()
                {
                    Type = x.OrganisationType.Name,
                    Name = x.Name,
                    SubCategory = x.Category.Name,
                    Category = x.Category.Category.Name,
                    Id = x.Id,
                }).ToListAsync();
        }
    }
}
