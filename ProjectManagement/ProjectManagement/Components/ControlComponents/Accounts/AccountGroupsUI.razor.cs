using Application.Feature.Account.Commands;
using Application.Feature.Account.Queries;
using BlazorMHD.UI.Core.Services;
using Domain.Entities.Calculation;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared.DTO.General;

namespace ProjectManagement.Components.ControlComponents.Accounts
{
    partial class AccountGroupsUI : IDisposable
    {
        int? GroupSelected;
        public List<ListDTO>? Groups;
        [Inject]
        DialogService DialogService { get; set; }
        async Task ChangeAccountGroupSelectedAsync(AccountGroupEntity ags)
        {
            GroupSelected = null;
            await Task.Delay(1);
            GroupSelected = ags.Id;
        }
        void ImportForm() => DialogService.ShowComponent<AccountImportFromFile>(
        AppLoc[LocalizerConst.Import, CalcResource.accountGroups], Icons.ImportFromFile);

        void UpdateForm(AccountGroupEntity model) =>
           DialogService.ShowComponent<AccountGroupsFormUI>(model.Id > 0 ? AppLoc[LocalizerConst.Update, model.Name] : AppLoc[LocalizerConst.New, CalcResource.accountGroups],
                new Dictionary<string, object>
                {
                    [nameof(AccountGroupsFormUI.AccountGroup)] = model,
                    [nameof(AccountGroupsFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, Callback)
                });
        async Task Callback(bool refresh)
        {
            if (refresh)
            {
                Groups = null;
                Groups = await MicroBus.Send(new GetAccountGroupsQuery());
            }
        }
        void Remove(ListDTO Organisation) => MHD.DeleteMessage(Organisation.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(Organisation)));
        async Task ConfirmRemoveAsync(ListDTO account)
        {
            var reault = await MicroBus.Send(new DeleteAccountGroupCommand(account.Id));
            if (reault)
            {
                Groups?.Remove(account);
                StateHasChanged();
            }
            MHD.Notifications(ToastType.Delete, reault);
        }
        protected async override Task OnInitializedAsync()
        {
            Groups = await MicroBus.Send(new GetAccountGroupsQuery());
        }

        public void Dispose()
        {
            //UoWService.Accounts.AccountHasChanged -= ModalChanged;
        }
    }
}
