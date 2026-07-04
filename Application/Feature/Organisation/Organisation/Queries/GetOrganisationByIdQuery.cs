using Application.Interfaces;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Feature.Organisation.Organisation.Queries
{
    // -------------------------
    // Details (OrganisationDetailsDTO)
    // -------------------------
    public sealed record GetOrganisationByIdQuery(int Id) : IRequest<OrganisationDetailsDTO?>;

    public sealed class GetOrganisationByIdQueryHandler(IOrganisationService service)
        : IRequestHandler<GetOrganisationByIdQuery, OrganisationDetailsDTO?>
    {
        public Task<OrganisationDetailsDTO?> Handle(GetOrganisationByIdQuery request, CancellationToken ct)
            => service.GetDetailsAsync(request.Id, ct);
    }

    // -------------------------
    // Post DTO (PostOrganisationDTO)
    // -------------------------
    public sealed record GetOrganisationToPostQuery(int Id) : IRequest<PostOrganisationDTO?>;

    public sealed class GetOrganisationToPostQueryHandler(IOrganisationService service)
        : IRequestHandler<GetOrganisationToPostQuery, PostOrganisationDTO?>
    {
        public Task<PostOrganisationDTO?> Handle(GetOrganisationToPostQuery request, CancellationToken ct)
            => service.GetPostAsync(request.Id, ct);
    }

    // -------------------------
    // List by Category (ShortListOrganisationDTO)
    // old: GetOrganisationsQuery(int GroupId, bool IsVisible)
    // -------------------------
    public sealed record GetOrganisationsQuery(int GroupId, bool IsVisible)
        : IRequest<List<ShortListOrganisationDTO>>;

    public sealed class GetOrganisationsQueryHandler(IOrganisationService service)
        : IRequestHandler<GetOrganisationsQuery, List<ShortListOrganisationDTO>>
    {
        public Task<List<ShortListOrganisationDTO>> Handle(GetOrganisationsQuery request, CancellationToken ct)
            => service.GetByCategoryAsync(request.GroupId, request.IsVisible, ct);
    }

    // -------------------------
    // Visible or Id (ListDTO)
    // old: GetVisibleOrIdQuery(int orgid) : IRequest<IEnumerable<ListDTO>>
    // -------------------------
    public sealed record GetVisibleOrIdQuery(int OrgId) : IRequest<List<ListDTO>>;

    public sealed class GetVisibleOrIdQueryHandler(IOrganisationService service)
        : IRequestHandler<GetVisibleOrIdQuery, List<ListDTO>>
    {
        public Task<List<ListDTO>> Handle(GetVisibleOrIdQuery request, CancellationToken ct)
            => service.GetVisibleOrIdAsync(request.OrgId <= 0 ? null : request.OrgId, ct);
    }

    // -------------------------
    // AsList (ListDTO)
    // (الملف القديم عندك كان فارغ تقريباً)
    // -------------------------
    public sealed record GetOrganisationsAsListQuery() : IRequest<List<ListDTO>>;

    public sealed class GetOrganisationsAsListQueryHandler(IOrganisationService service)
        : IRequestHandler<GetOrganisationsAsListQuery, List<ListDTO>>
    {
        public Task<List<ListDTO>> Handle(GetOrganisationsAsListQuery request, CancellationToken ct)
            => service.GetAsListAsync(ct);
    }

    // -------------------------
    // Flat rows for "Kunder & leverantörer" table (OrganisationRowDTO)
    // -------------------------
    public sealed record GetOrganisationRowsQuery(bool IncludeArchived) : IRequest<List<OrganisationRowDTO>>;

    public sealed class GetOrganisationRowsQueryHandler(IOrganisationService service)
        : IRequestHandler<GetOrganisationRowsQuery, List<OrganisationRowDTO>>
    {
        public Task<List<OrganisationRowDTO>> Handle(GetOrganisationRowsQuery request, CancellationToken ct)
            => service.GetRowsAsync(request.IncludeArchived, ct);
    }

    // -------------------------
    // Dubblettkontroll: liknande namn (ListDTO)
    // -------------------------
    public sealed record FindSimilarOrganisationsQuery(string Name) : IRequest<List<ListDTO>>;

    public sealed class FindSimilarOrganisationsQueryHandler(IOrganisationService service)
        : IRequestHandler<FindSimilarOrganisationsQuery, List<ListDTO>>
    {
        public Task<List<ListDTO>> Handle(FindSimilarOrganisationsQuery request, CancellationToken ct)
            => service.FindSimilarByNameAsync(request.Name, ct);
    }
}
