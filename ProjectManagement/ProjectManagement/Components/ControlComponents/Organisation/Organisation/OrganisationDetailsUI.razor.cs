using Application.Feature.Organisation.Organisation.Queries;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Client.Shared.ResourceFiles.Identity;
using ProjectManagement.Shared;
using ProjectManagement.Shared.Base.Organisation;
using ProjectManagement.Shared.DTO.App.List;
using ProjectManagement.Shared.DTO.Organisation;

namespace ProjectManagement.Components.ControlComponents.Organisation.Organisation
{
    public partial class OrganisationDetailsUI : ComponentBase
    {
        [Inject] public ICommandDispatcher MicroBus { get; set; } = default!;
        [Inject] public ContextMenuService ContextService { get; set; } = default!;
        [Inject] public MhdServices MHD { get; set; } = default!;
        [Inject] public IStringLocalizer<ResourceApp> AppLoc { get; set; } = default!;
        [Inject] public IStringLocalizer<PMWebResource> WebLoc { get; set; } = default!;

        private int Part = 1;

        internal enum DetailSubTab { Contacts, Addresses }
        private DetailSubTab _subTab = DetailSubTab.Contacts;

        [Parameter] public bool IsLoading { get; set; }
        [Parameter] public OrganisationDetailsDTO? Company { get; set; }
        [Parameter] public int CompanyId { get; set; }
        [Parameter] public EventCallback Callback { get; set; }

        private List<TabItem> Tabs { get; set; } = [];

        protected override async Task OnParametersSetAsync()
        {
            // Same main structure as the create/edit form, in read mode.
            Tabs =
            [
                new(1, "Grundinformation"),
                new(2, "Värdering"),
                new(3, "Kontakt & adress"),
            ];

            if (CompanyId <= 0) return;
            Company = await MicroBus.Send(new GetOrganisationByIdQuery(CompanyId));
        }

        private void Close() => MHD.Modal.CloseAsync();

        private string SubTabClass(DetailSubTab tab)
            => _subTab == tab
                ? "rounded-md px-3 py-1.5 text-sm font-semibold transition bg-slate-200 text-slate-900 dark:bg-slate-700 dark:text-slate-100"
                : "rounded-md px-3 py-1.5 text-sm font-medium text-slate-500 transition hover:bg-slate-100 hover:text-slate-700 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-slate-200";

        private static string Dash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

        private static string DateOrDash(DateTime? value)
            => value is null ? "—" : value.Value.ToString("yyyy-MM-dd");

        private static string DateTimeOrDash(DateTime? value)
            => value is null ? "—" : value.Value.ToString("yyyy-MM-dd HH:mm");

        // Older posts stored structured address fields — compose them into one readable line.
        private static string AddressLine(ProjectManagement.Shared.DTO.App.AddressDTO a)
        {
            var parts = new[]
                {
                    $"{a.Street} {a.Nr}".Trim(),
                    $"{a.ZIPCode} {a.City}".Trim(),
                    a.Region?.Trim() ?? string.Empty,
                    a.Country?.Trim() ?? string.Empty
                }
                .Where(p => !string.IsNullOrWhiteSpace(p));

            return string.Join(", ", parts);
        }

        // Closed-state one-liner: "Senast ändrad 2026-07-07 13:10 av Nordbygg Admin".
        private string SystemInfoSummary()
        {
            if (Company is null)
                return string.Empty;

            var at = Company.UpdatedAt ?? Company.CreatedAt;
            var by = !string.IsNullOrWhiteSpace(Company.UpdatedByName) ? Company.UpdatedByName : Company.CreatedByName;
            if (at is null && string.IsNullOrWhiteSpace(by))
                return string.Empty;

            var text = $"Senast ändrad {DateTimeOrDash(at)}";
            return string.IsNullOrWhiteSpace(by) ? text : $"{text} av {by}";
        }

        private static string ReviewLabel(YesNoUnkown value) => value switch
        {
            YesNoUnkown.notSpecified => "Ej angivet",
            YesNoUnkown.yes => "Ja",
            YesNoUnkown.no => "Nej",
            YesNoUnkown.notRelevant => "Ej relevant",
            YesNoUnkown.unkown => "Okänd",
            _ => "Ej angivet"
        };

        private static MarkupString StatusBadge(string? status)
        {
            var label = string.IsNullOrWhiteSpace(status) ? OrganisationStatusCatalog.NotSpecified : OrganisationStatusCatalog.Normalize(status);
            return (MarkupString)$"<span class=\"inline-flex items-center rounded-full px-2 py-0.5 text-xs font-semibold ring-1 ring-inset {OrganisationStatusCatalog.BadgeClasses(label)}\">{System.Net.WebUtility.HtmlEncode(label)}</span>";
        }
    }
}
