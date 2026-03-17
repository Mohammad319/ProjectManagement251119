using Application.Feature.Organisation.Organisation.Commands;
using Application.Feature.Organisation.Organisation.Queries;
using Application.Feature.Organisation.OrganisationCategory.Queries;
using Application.Feature.Organisation.OrganisationType.Queries;
using Domain.DTO.Category;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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
        private PostOrganisationDTO PostCompany { get; set; } = new();
        private List<ListOrganisationCategoryDTO> Categories { get; set; } = [];
        private ListOrganisationCategoryDTO? CategorySelected { get; set; }
        private IEnumerable<ListDTO> CustomerGroups { get; set; } = [];

        private List<TabItem> Tabs { get; set; } = [];

        protected override async Task OnInitializedAsync()
        {
            Tabs =
            [
                new(1, WebLoc[nameof(PMWebResource.BasicInformation)]),
                new(2, ResourceApp.Assessment),
                new(3, WebLoc[nameof(PMWebResource.Category)]),
                new(4, ResourceIdentity.contact),
            ];

            Categories = await MicroBus.Send(new GetOrganisationCategoryQuery()) ?? [];

            if (ID > 0)
                PostCompany = await MicroBus.Send(new GetOrganisationToPostQuery(ID)) ?? new PostOrganisationDTO();
            else
                PostCompany = new PostOrganisationDTO { CategoryId = CategoryID };

            EnsureCollections();
            ResolveSelectedBaseCategory();

            CustomerGroups = await MicroBus.Send(new GetListOrganisationsTypeQuery(PostCompany.OrganisationTypeID, ID > 0 ? ID : null)) ?? [];
        }

        private void EnsureCollections()
        {
            PostCompany.Notes ??= [];
            PostCompany.Contacts ??= [];
            PostCompany.Address ??= new();
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

        private void CategoryChange(ChangeEventArgs e)
        {
            if (e.Value is null)
            {
                CategorySelected = null;
                PostCompany.CategoryId = 0;
                return;
            }

            if (!int.TryParse(e.Value.ToString(), out var id) || id <= 0)
            {
                CategorySelected = null;
                PostCompany.CategoryId = 0;
                return;
            }

            CategorySelected = Categories.FirstOrDefault(x => x.Id == id);
            PostCompany.CategoryId = 0;
        }

        private async Task HandleSubmitAsync(EditContext editContext)
        {
            if (IsLoading) return;

            ValidateNameOnly();
            if (!string.IsNullOrWhiteSpace(NameValidationError))
                return;

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
                ? ResLocalize.FieldIsRequred
                : null;
        }
    }
}
