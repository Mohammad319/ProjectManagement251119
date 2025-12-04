using Application.Interfaces;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Feature.Organisation.OrganisationCategory.Queries
{
    public sealed record GetOrganisationCategoryQuery() : IRequest<List<OrganisationCategoryEntity>>;
    public class GetCompanyCategoryQueryHandler(IShardingSingleDbContext _context) : IRequestHandler<GetOrganisationCategoryQuery, List<OrganisationCategoryEntity>>
    {
        public async Task<List<OrganisationCategoryEntity>> Handle(GetOrganisationCategoryQuery query, CancellationToken cancellationToken)
        {
            var list = await _context.OrganisationCategory.ToListAsync(cancellationToken);
            return list;
        }
    }
}
