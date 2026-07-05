using Application.Feature.Organisation.Organisation.Commands;
using Application.Feature.Organisation.Organisation.Queries;
using Application.Feature.Organisation.OrganisationCategory.Queries;
using Application.Feature.Organisation.OrganisationType.Queries;
using Domain.DTO.Category;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ProjectManagement.Client.Shared.SharedComponent;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Client.Shared.ResourceFiles.Identity;
using ProjectManagement.Shared;
using ProjectManagement.Shared.Base.Organisation;
using ProjectManagement.Shared.DTO.App.List;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Organisation;
using ProjectManagement.Shared.Resource;

namespace ProjectManagement.Components.ControlComponents.Organisation.Organisation
{
    public partial class OrganisationFormUI
    {
        public const string DialogFormId = "organisationForm";
        [Inject] ICommandDispatcher MicroBus { get; set; } = default!;
        [Inject] ContextMenuService ContextService { get; set; } = default!;
        [Inject] MhdServices MHD { get; set; } = default!;
        [Inject] IStringLocalizer<ResourceApp> AppLoc { get; set; } = default!;
        [Inject] IStringLocalizer<PMWebResource> WebLoc { get; set; } = default!;

        private int Part = 1;

        [Parameter] public EventCallback<bool> Callback { get; set; }
        [Parameter] public int ID { get; set; }
        [Parameter] public int CategoryID { get; set; }

        private bool IsLoading;
        private string? NameValidationError;
        private string? WarningReasonValidationError;
        private PostOrganisationDTO PostCompany { get; set; } = new();
        private List<ListOrganisationCategoryDTO> Categories { get; set; } = [];
        private ListOrganisationCategoryDTO? CategorySelected { get; set; }
        private IEnumerable<ListDTO> CustomerGroups { get; set; } = [];

        private List<TabItem> Tabs { get; set; } = [];

        protected override async Task OnInitializedAsync()
        {
            Tabs =
            [
                new(1, "Grundinformation"),
                new(2, "Värdering"),
                new(3, "Grupp"),
                new(4, "Kontakt"),
            ];

            Categories = await MicroBus.Send(new GetOrganisationCategoryQuery()) ?? [];

            if (ID > 0)
                PostCompany = await MicroBus.Send(new GetOrganisationToPostQuery(ID)) ?? new PostOrganisationDTO();
            else
                PostCompany = new PostOrganisationDTO { CategoryId = CategoryID, Status = OrganisationStatusCatalog.Active };

            EnsureCollections();
            EnsureDefaults();
            ResolveSelectedBaseCategory();

            CustomerGroups = await MicroBus.Send(new GetListOrganisationsTypeQuery(PostCompany.OrganisationTypeID, ID > 0 ? ID : null)) ?? [];
        }

        private void EnsureCollections()
        {
            PostCompany.Notes ??= [];
            PostCompany.Contacts ??= [];
            PostCompany.Address ??= new();
        }

        private void EnsureDefaults()
        {
            if (string.IsNullOrWhiteSpace(PostCompany.Status))
                PostCompany.Status = OrganisationStatusCatalog.Active;
        }

        private int? SelectedMainGroupId
        {
            get => CategorySelected?.Id;
            set
            {
                CategorySelected = value is int id && id > 0
                    ? Categories.FirstOrDefault(x => x.Id == id)
                    : null;
                PostCompany.CategoryId = 0;
            }
        }

        private bool IsWarningStatus =>
            string.Equals(PostCompany.Status, OrganisationStatusCatalog.Warning, StringComparison.OrdinalIgnoreCase);

        private IReadOnlyList<MhdSelectItem<int?>> OrganisationTypeOptions =>
            CustomerGroups.Select(x => new MhdSelectItem<int?> { Value = x.Id, Label = x.Name }).ToList();

        private IReadOnlyList<MhdSelectItem<string>> StatusOptions =>
            OrganisationStatusCatalog.FixedStatuses
                .Select(x => new MhdSelectItem<string> { Value = x, Label = x, Color = StatusColor(x) })
                .ToList();

        private IReadOnlyList<MhdSelectItem<YesNoUnkown>> ReviewOptions =>
        [
            new() { Value = YesNoUnkown.notSpecified, Label = "Ej angivet" },
            new() { Value = YesNoUnkown.yes, Label = "Ja" },
            new() { Value = YesNoUnkown.no, Label = "Nej" },
            new() { Value = YesNoUnkown.notRelevant, Label = "Ej relevant" },
            new() { Value = YesNoUnkown.unkown, Label = "Okänd" }
        ];

        private IReadOnlyList<MhdSelectItem<int?>> MainGroupOptions =>
            Categories
                .Where(x => x.ParentCategoryId is null)
                .OrderBy(x => x.Name)
                .Select(x => new MhdSelectItem<int?> { Value = x.Id, Label = x.Name })
                .ToList();

        private IReadOnlyList<MhdSelectItem<int>> SubGroupOptions =>
            CategorySelected is null
                ? []
                : Categories
                    .Where(x => x.ParentCategoryId == CategorySelected.Id)
                    .OrderBy(x => x.Name)
                    .Select(x => new MhdSelectItem<int> { Value = x.Id, Label = x.Name })
                    .ToList();

        private static string StatusColor(string status) => status switch
        {
            OrganisationStatusCatalog.Active => "#16a34a",
            OrganisationStatusCatalog.UnderReview => "#0284c7",
            OrganisationStatusCatalog.Approved => "#15803d",
            OrganisationStatusCatalog.NotApproved => "#dc2626",
            OrganisationStatusCatalog.Paused => "#f97316",
            OrganisationStatusCatalog.Archived => "#64748b",
            OrganisationStatusCatalog.Warning => "#f59e0b",
            _ => "#94a3b8"
        };

        private void OnStatusChanged(string status)
        {
            PostCompany.Status = OrganisationStatusCatalog.Normalize(status);
            if (!IsWarningStatus)
                WarningReasonValidationError = null;
        }

        private void ResolveSelectedBaseCategory()
        {
            var selectedCategoryId = PostCompany.CategoryId > 0 ? PostCompany.CategoryId : CategoryID;
            if (selectedCategoryId <= 0)
            {
                CategorySelected = null;
                return;
            }

            var selected = Categories.FirstOrDefault(x => x.Id == selectedCategoryId);
            if (selected is null)
            {
                CategorySelected = null;
                return;
            }

            CategorySelected = selected.ParentCategoryId is null
                ? selected
                : Categories.FirstOrDefault(x => x.Id == selected.ParentCategoryId);
        }

        private void AddNote() => PostCompany.Notes.Add(string.Empty);

        private void RemoveNote(int noteIndex)
        {
            if (noteIndex < 0 || noteIndex >= PostCompany.Notes.Count)
                return;

            PostCompany.Notes.RemoveAt(noteIndex);
        }

        // Dubblettkontroll: liknande namn som redan finns, plus ett flagga när användaren
        // uttryckligen valt att skapa ändå.
        private List<ListDTO>? SimilarOrganisations;
        private bool DuplicateConfirmed;

        private async Task HandleSubmitAsync(EditContext editContext) => await TrySaveAsync();

        private async Task TrySaveAsync()
        {
            if (IsLoading) return;

            ValidateNameOnly();
            ValidateWarningReason();
            if (!string.IsNullOrWhiteSpace(NameValidationError) || !string.IsNullOrWhiteSpace(WarningReasonValidationError))
                return;

            // Endast vid nyskapande, och bara tills användaren bekräftat att det är ett annat företag.
            if (ID == 0 && !DuplicateConfirmed)
            {
                var similar = await MicroBus.Send(new FindSimilarOrganisationsQuery(PostCompany.Name)) ?? [];
                if (similar.Count > 0)
                {
                    SimilarOrganisations = similar;
                    return;
                }
            }

            await PersistAsync();
        }

        private async Task CreateAnywayAsync()
        {
            DuplicateConfirmed = true;
            SimilarOrganisations = null;
            await PersistAsync();
        }

        private async Task PersistAsync()
        {
            IsLoading = true;
            try
            {
                PostOrganisationDTO dto = new();
                PropertyCopier.CopyPropertiesTo(PostCompany, dto);

                var result =
                    (ID > 0 && await MicroBus.Send(new UpdateOrganisationCommand(dto, ID))) ||
                    (ID == 0 && await MicroBus.Send(new CreateOrganisationCommand(dto)) > 0);

                MHD.Notifications(ID > 0 ? ToastType.Update : ToastType.Add, result);
                await Callback.InvokeAsync(result);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ValidateNameOnly()
        {
            NameValidationError = string.IsNullOrWhiteSpace(PostCompany.Name)
                ? string.Format(ResLocalize.FieldIsRequred, nameof(PostCompany.Name))
                : null;
        }

        private void ValidateWarningReason()
        {
            WarningReasonValidationError =
                IsWarningStatus && string.IsNullOrWhiteSpace(PostCompany.WarningReason)
                    ? "Orsak till varning är obligatorisk när status är Varning."
                    : null;
        }
    }
}
