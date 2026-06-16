using Application.Feature.Organisation.Organisation.Commands;
using Application.Feature.Organisation.Organisation.Queries;
using Domain.DTO.Category;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Organisation;

namespace ProjectManagement.Components.ControlComponents.Organisation.Organisation
{
    public partial class OrganisationsUI
    {
        [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Parameter] public ListOrganisationCategoryDTO? Category { get; set; }
        [Parameter] public EventCallback Callback { get; set; }

        private bool IsVisible = true;
        private List<ShortListOrganisationDTO> Organistion { get; set; } = [];

        private int? _lastCategoryId;

        protected override async Task OnParametersSetAsync()
        {
            if (Category?.Id == _lastCategoryId) return;

            _lastCategoryId = Category?.Id;
            await GetAsync();
        }

        private async Task GetAsync()
        {
            if (Category?.Id is null || Category.Id <= 0)
            {
                Organistion = [];
                return;
            }

            Organistion = await Dispatcher.Send(new GetOrganisationsQuery(Category.Id, IsVisible)) ?? [];
        }

        private async Task<bool> CanManageAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            if (user.Identity?.IsAuthenticated != true)
                return false;

            return PMRolesConst.Tenant.AdminManger
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(user.IsInRole);
        }

        private async Task Context(ShortListOrganisationDTO item)
        {
            var list = new List<MhdContextMenuItem>
            {
                new()
                {
                    Label = $"ℹ️ {ResourceLoc.details}",
                    OnClickAsync = () =>
                    {
                        OrganisationDetails(item);
                        return Task.CompletedTask;
                    }
                }
            };

            if (await CanManageAsync())
            {
                list.Add(new()
                {
                    Label = $"✏️ {AppLoc[nameof(ResourceApp.edit)]}",
                    OnClickAsync = () =>
                    {
                        UpdateForm(item);
                        return Task.CompletedTask;
                    }
                });

                list.Add(new()
                {
                    Label = $"🗑️ {AppLoc[nameof(ResourceApp.delete)]}",
                    OnClickAsync = () =>
                    {
                        Remove(item);
                        return Task.CompletedTask;
                    }
                });
            }

            await ContextService.ShowMenuAsync(list);
        }

        private void OrganisationDetails(ShortListOrganisationDTO obj)
        {
            if (obj.Id <= 0)
                return;

            MHD.Modal.ShowComponent<OrganisationDetailsUI>(
                obj.Name,
                new Dictionary<string, object>
                {
                    [nameof(OrganisationDetailsUI.CompanyId)] = obj.Id
                },
                BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge);
        }

        private void UpdateForm(ShortListOrganisationDTO model)
        {
            if (Category?.Id is not > 0)
                return;

            MHD.Modal.ShowComponent<OrganisationFormUI>(
                model.Id != 0
                    ? AppLoc[LocalizerConst.Update, model.Name]
                    : AppLoc[LocalizerConst.New, AppLoc[nameof(ResourceApp.organisation)]],
                new Dictionary<string, object>
                {
                    [nameof(OrganisationFormUI.ID)] = model.Id,
                    [nameof(OrganisationFormUI.CategoryID)] = Category.Id,
                    [nameof(OrganisationFormUI.Callback)] =
                        EventCallback.Factory.Create<bool>(this, RefreshAsync)
                },
                BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge,
                DialogButtonsHelper.CreateSaveCancelButtons(OrganisationFormUI.DialogFormId));
        }

        private void Remove(ShortListOrganisationDTO organisation)
        {
            MHD.DeleteMessage(
                organisation.Name,
                EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(organisation)));
        }

        private async Task ConfirmRemoveAsync(ShortListOrganisationDTO organisation)
        {
            var result = await Dispatcher.Send(
                new DeleteOrganisationCommand(organisation.Id));

            if (result)
                Organistion.RemoveAll(x => x.Id == organisation.Id);

            MHD.Notifications(ToastType.Delete, result);
            await InvokeAsync(StateHasChanged);
        }

        private async Task ReverseElements()
        {
            IsVisible = !IsVisible;
            await GetAsync();
        }

        private async Task RefreshAsync(bool load)
        {
            if (load)
                await GetAsync();

            await MHD.Modal.CloseAsync();
            await InvokeAsync(StateHasChanged);
        }
    }
}
