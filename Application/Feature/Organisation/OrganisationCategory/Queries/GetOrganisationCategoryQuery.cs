using Application.Interfaces;
using Domain.DTO.Category;

namespace Application.Feature.Organisation.OrganisationCategory.Queries
{
    public sealed record GetOrganisationCategoryQuery() : IRequest<List<ListOrganisationCategoryDTO>>;

    public sealed class GetOrganisationCategoryQueryHandler(IOrganisationCategoryService service)
        : IRequestHandler<GetOrganisationCategoryQuery, List<ListOrganisationCategoryDTO>>
    {
        public Task<List<ListOrganisationCategoryDTO>> Handle(GetOrganisationCategoryQuery request, CancellationToken ct)
            => service.GetAllAsync(ct);
    }
}
