using Application.Interfaces;
using Domain.DTO.Category;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Organisation.OrganisationType.Queries
{
    public sealed record GetAllOrganisationsTypeQuery(bool IsVisible)
        : IRequest<List<ListOrganisationTypeDTO>>;

    public sealed class GetAllOrganisationsTypeQueryHandler(IOrganisationTypeService service)
        : IRequestHandler<GetAllOrganisationsTypeQuery, List<ListOrganisationTypeDTO>>
    {
        public Task<List<ListOrganisationTypeDTO>> Handle(GetAllOrganisationsTypeQuery request, CancellationToken ct)
            => service.GetAllAsync(request.IsVisible, ct);
    }

    public sealed record GetListOrganisationsTypeQuery(int? GroupID, int? CustomerID)
        : IRequest<List<ListDTO>>;

    public sealed class GetListOrganisationsTypeQueryHandler(IOrganisationTypeService service)
        : IRequestHandler<GetListOrganisationsTypeQuery, List<ListDTO>>
    {
        public Task<List<ListDTO>> Handle(GetListOrganisationsTypeQuery request, CancellationToken ct)
            => service.GetAsListAsync(request.GroupID, request.CustomerID, ct);
    }
}
