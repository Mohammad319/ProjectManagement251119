using Application.Feature.Account.Commands;
using Application.Feature.Account.Queries;
using BlazorMHD.UI.Core.Services;
using ContextMenuMHD;
using Domain.Entities.Calculation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ProjectManagement.Client.Services.MHDBlazor;
using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared.DTO.General;

namespace ProjectManagement.Components.ControlComponents.Accounts;

public partial class AccountGroupsUI : IDisposable
{
    private int? GroupSelected;
    private List<ListDTO>? Groups;

    [Inject] private DialogService DialogService { get; set; } = default!;
    [Inject] private IStringLocalizer<ResourceApp> AppLoc { get; set; } = default!;
    [Inject] private ICommandDispatcher MicroBus { get; set; } = default!;
    [Inject] private MhdServices MHD { get; set; } = default!;
    [Inject] private ContextMenuService ContextService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        Groups = await MicroBus.Send(new GetAccountGroupsQuery());
    }

    private async Task ChangeAccountGroupSelectedAsync(ListDTO ags)
    {
        GroupSelected = null;
        await InvokeAsync(StateHasChanged);
        GroupSelected = ags.Id;
    }

    private void ImportForm() =>
        DialogService.ShowComponent<AccountImportFromFile>(
            AppLoc[LocalizerConst.Import, CalcResource.accountGroups],
            Icons.ImportFromFile);

    private void UpdateForm(AccountGroupEntity model) =>
        DialogService.ShowComponent<AccountGroupsFormUI>(
            model.Id > 0
                ? AppLoc[LocalizerConst.Update, model.Name]
                : AppLoc[LocalizerConst.New, CalcResource.accountGroups],
            new Dictionary<string, object>
            {
                [nameof(AccountGroupsFormUI.AccountGroup)] = model,
                [nameof(AccountGroupsFormUI.OnSaved)] = EventCallback.Factory.Create<bool>(this, Callback)
            });

    private async Task Callback(bool refresh)
    {
        if (!refresh) return;

        Groups = null;
        Groups = await MicroBus.Send(new GetAccountGroupsQuery());
        await InvokeAsync(StateHasChanged);
    }

    private void Remove(ListDTO organisation) =>
        MHD.DeleteMessage(organisation.Name,
            EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(organisation)));

    private async Task ConfirmRemoveAsync(ListDTO account)
    {
        var result = await MicroBus.Send(new DeleteAccountGroupCommand(account.Id));
        if (result)
        {
            Groups?.Remove(account);
            await InvokeAsync(StateHasChanged);
        }

        MHD.Notifications(ToastType.Delete, result);
    }

    private async Task GroupContextM(ListDTO item)
    {
        await ContextService.ShowMenuAsync(new()
        {
            new MenuItem
            {
                Label = $"✏️ {ResourceApp.update}",
                OnClickAsync = () =>
                {
                    UpdateForm(new AccountGroupEntity(item.Name) { Id = item.Id});
                    return Task.CompletedTask;
                }
            },
            new MenuItem
            {
                Label = $"🗑️ {ResourceApp.delete}",
                OnClickAsync = () =>
                {
                    Remove(item);
                    return Task.CompletedTask;
                }
            }
        });

        GroupSelected = item.Id;
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        // لا شيء حاليًا
    }
}
