using Application.Feature.Organisation.Organisation.Queries;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Client.Shared.ResourceFiles.Identity;
using ProjectManagement.Shared;
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

        [Parameter] public bool IsLoading { get; set; }
        [Parameter] public OrganisationDetailsDTO? Company { get; set; }
        [Parameter] public int CompanyId { get; set; }
        [Parameter] public EventCallback Callback { get; set; }

        private List<TabItem> Tabs { get; set; } = [];

        protected override async Task OnParametersSetAsync()
        {
            Tabs =
            [
                new(1, WebLoc[nameof(PMWebResource.BasicInformation)]),
                new(2, ResourceApp.Assessment),
                new(3, WebLoc[nameof(PMWebResource.Category)]),
                new(4, ResourceIdentity.contact),
            ];

            if (CompanyId <= 0) return;
            Company = await MicroBus.Send(new GetOrganisationByIdQuery(CompanyId));
        }

        private void Close() => MHD.Modal.Close();
    }
}
