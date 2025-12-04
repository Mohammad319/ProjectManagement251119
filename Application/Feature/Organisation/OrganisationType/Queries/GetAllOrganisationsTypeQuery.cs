using Application.Interfaces;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Feature.Organisation.OrganisationType.Queries
{
    public sealed record GetAllOrganisationsTypeQuery(bool IsVisible) : IRequest<List<OrganisationTypeEntity>>;
        public class GetAllOrganisationsTypeQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetAllOrganisationsTypeQuery, List<OrganisationTypeEntity>>
        {
            public async Task<List<OrganisationTypeEntity>> Handle(GetAllOrganisationsTypeQuery query, CancellationToken cancellationToken)
            {
                return await context.OrganisationType.Where(x => x.IsVisible == query.IsVisible).ToListAsync(cancellationToken);
            }
        }
    }
