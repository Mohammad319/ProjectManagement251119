using Application.Interfaces;

namespace Application.Feature.General.DropdownSettings.Queries
{
    public sealed record GetDropdownRequirementsQuery() : IRequest<Dictionary<string, bool>>;

    public sealed class GetDropdownRequirementsQueryHandler(IDropdownSettingService service)
        : IRequestHandler<GetDropdownRequirementsQuery, Dictionary<string, bool>>
    {
        public Task<Dictionary<string, bool>> Handle(GetDropdownRequirementsQuery request, CancellationToken ct)
            => service.GetRequirementsAsync(ct);
    }
}
