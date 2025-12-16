using Application.Feature.Organisation.Organisation.Commands;
using Application.Feature.Organisation.Organisation.Queries;
using Application.Feature.Organisation.OrganisationCategory.Queries;
using Application.Feature.Organisation.OrganisationType.Queries;
using Domain.DTO.Category;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Client.Shared.ResourceFiles.Identity;
using ProjectManagement.Shared.Base.Organisation;
using ProjectManagement.Shared.DTO.App.List;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Organisation;

namespace ProjectManagement.Components.ControlComponents.Organisation.Organisation
{
    public partial class OrganisationFormUI
    {
        [Inject] public ICommandDispatcher MicroBus { get; set; } = default!;
        [Inject] public ContextMenuService ContextService { get; set; } = default!;
        [Inject] public MhdServices MHD { get; set; } = default!;
        [Inject] public IStringLocalizer<ResourceApp> AppLoc { get; set; } = default!;

        private int Part = 1;

        [Parameter] public EventCallback<bool> Callback { get; set; }
        [Parameter] public int ID { get; set; }
        [Parameter] public int CategoryID { get; set; }

        private bool IsLoading;
        private PostOrganisationDTO PostCompany { get; set; } = new();
        private List<ListOrganisationCategoryDTO> Categories { get; set; } = [];
        private ListOrganisationCategoryDTO? CategorySelected { get; set; }
        private IEnumerable<ListDTO> CustomerGroups { get; set; } = [];

        private static readonly List<TabItem> Tabs = new()
        {
            new(1, "Basic information"),
            new(2, ResourceApp.Assessment),
            new(3, "Category"),
            new(4, ResourceIdentity.contact),
        };

        protected override async Task OnInitializedAsync()
        {
            Categories = await MicroBus.Send(new GetOrganisationCategoryQuery()) ?? [];

            if (ID > 0)
                PostCompany = await MicroBus.Send(new GetOrganisationToPostQuery(ID)) ?? new PostOrganisationDTO();
            else
                PostCompany = new PostOrganisationDTO { CategoryId = CategoryID };

            // تحديد الـ Base category من CategoryID
            if (CategoryID > 0)
            {
                var sub = Categories.FirstOrDefault(x => x.Id == CategoryID);
                if (sub?.ParentCategoryId is not null)
                    CategorySelected = Categories.FirstOrDefault(x => x.Id == sub.ParentCategoryId);
            }

            CustomerGroups = await MicroBus.Send(new GetListOrganisationsTypeQuery(PostCompany.OrganisationTypeID, ID > 0 ? ID : null)) ?? [];
        }

        private void CategoryChange(ChangeEventArgs e)
        {
            if (e.Value is null) { CategorySelected = null; return; }

            if (!int.TryParse(e.Value.ToString(), out var id) || id <= 0)
            {
                CategorySelected = null;
                PostCompany.CategoryId = 0;
                return;
            }

            CategorySelected = Categories.FirstOrDefault(x => x.Id == id);
            PostCompany.CategoryId = 0; // reset sub category selection
        }

        private async Task HandleSubmitAsync()
        {
            if (IsLoading) return;

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

        // إذا تحتاج هذي لاحقاً ابقها، لكن حالياً غير مستخدمة
        private UnderContactOrganisationBase? underContact;

        private void Add()
        {
            if (underContact is null) return;
            PostCompany.Contacts.Add(underContact);
            underContact = null;
        }

        private void Remove(UnderContactOrganisationBase contact) =>
            PostCompany.Contacts.Remove(contact);
    }
}
