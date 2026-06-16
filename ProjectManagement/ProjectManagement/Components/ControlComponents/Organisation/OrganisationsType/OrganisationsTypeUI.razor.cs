using Application.Feature.Organisation.OrganisationType.Commands;
using Application.Feature.Organisation.OrganisationType.Queries;
using Domain.DTO.Category;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Client.Shared.ResourceFiles.Identity;
using ProjectManagement.Shared.DTO.Organisation;

namespace ProjectManagement.Components.ControlComponents.Organisation.OrganisationsType
{
    public class OrganisationsTypeUiBase : ComponentBase
    {
        [Inject] protected ICommandDispatcher MicroBus { get; set; } = default!;
        [Inject] protected MhdServices MHD { get; set; } = default!;
        [Inject] protected IStringLocalizer<ResourceApp> AppLoc { get; set; } = default!;

        protected List<ListOrganisationTypeDTO>? OrganisationTypes;
        protected bool IsLoading;
        protected bool IsVisible = true;

        protected override async Task OnInitializedAsync()
            => await ReloadAsync();

        protected void NewOrganisationType()
            => OpenModal(new ListOrganisationTypeDTO { IsVisible = true });

        protected void OpenEditModal(ListOrganisationTypeDTO item)
            => OpenModal(item);

        protected void OpenModal(ListOrganisationTypeDTO model)
        {
            var title = model.Id == 0
                ? AppLoc[LocalizerConst.New, ResourceIdentity.customerGroup]
                : AppLoc[LocalizerConst.Update, model.Name];

            MHD.Modal.ShowComponent<OrganisationTypeFormUI>(
                title,
                new Dictionary<string, object>
                {
                    [nameof(OrganisationTypeFormUI.IsLoading)] = IsLoading,
                    [nameof(OrganisationTypeFormUI.CustomerGroup)] = model,
                    [nameof(OrganisationTypeFormUI.OnValidSubmit)] =
                        EventCallback.Factory.Create<ListOrganisationTypeDTO>(this, HandleSubmitAsync),
                });
        }

        protected async Task HandleSubmitAsync(ListOrganisationTypeDTO model)
        {
            if (IsLoading || model is null) return;

            IsLoading = true;
            try
            {
                PostOrganisationTypeDTO entity = new();
                PropertyCopier.CopyPropertiesTo(model, entity);

                bool ok;
                if (model.Id == 0)
                    ok = await MicroBus.Send(new CreateOrganisationTypeCommand(entity)) > 0;
                else
                    ok = await MicroBus.Send(new UpdateOrganisationTypeCommand(entity, model.Id));

                if (ok)
                    await ReloadAsync();

                MHD.Notifications(model.Id == 0 ? ToastType.Add : ToastType.Update, ok);
            }
            finally
            {
                IsLoading = false;
                await MHD.Modal.CloseAsync();
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void AskDelete(ListOrganisationTypeDTO item)
        {
            if (item is null) return;

            MHD.DeleteMessage(
                item.Name,
                EventCallback.Factory.Create(this, () => ConfirmDeleteAsync(item)));
        }

        protected async Task ConfirmDeleteAsync(ListOrganisationTypeDTO item)
        {
            if (IsLoading || item is null) return;

            IsLoading = true;
            try
            {
                var ok = await MicroBus.Send(new DeleteOrganisationTypeCommand(item.Id));
                if (ok)
                {
                    OrganisationTypes?.RemoveAll(x => x.Id == item.Id);
                    await InvokeAsync(StateHasChanged);
                }

                MHD.Notifications(ToastType.Delete, ok);
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task ToggleVisibilityAsync()
        {
            if (IsLoading)
                return;

            IsVisible = !IsVisible;
            await ReloadAsync();
        }

        protected async Task ReloadAsync()
        {
            IsLoading = true;
            try
            {
                OrganisationTypes = await MicroBus.Send(new GetAllOrganisationsTypeQuery(IsVisible)) ?? [];
            }
            finally
            {
                IsLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
