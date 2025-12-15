using Application.Feature.Account.Queries;
using Application.Feature.Calculation.ResourceType.Commands;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.ResourceType;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Components.ControlComponents.ResourceType
{
    public partial class ResourceTypeFormUI
    {
        [Parameter] public EventCallback<bool> Callback { get; set; }
        [Parameter] public ResourceTypeModel ResourceType { get; set; } = new();
        PostResourceTypeDTO ResourceTypeUpdate { get; set; } = new();
        bool IsLoading = false;
        List<ListAccountGroupIncludeAccountDTO>? AccountGroups;
        ListAccountGroupIncludeAccountDTO? AccountGroupSelected;

        async Task ChangeGroupAccount(ChangeEventArgs e)
        {
            AccountGroupSelected = null;
            int id = int.Parse(e.Value.ToString());
            await Task.Delay(1);
            if (id == 0) return;
            SelectedGroup(id);
        }
        void SelectedGroup(int id) => AccountGroupSelected = AccountGroups?.FirstOrDefault(x => x.Id == id);
        protected async override Task OnInitializedAsync()
        {
            PropertyCopier.CopyPropertiesTo(ResourceType, ResourceTypeUpdate);
            AccountGroups = await MicroBus.Send(new GetAccountGroupsAsListQuery());
            if (ResourceType.Id > 0)
            {
                AccountGroupSelected = AccountGroups.FirstOrDefault(x => x.Accounts.Any(a => a.Id == ResourceTypeUpdate.AccountId));
            }
        }
        private async Task HandleSubmitAsync()
        {
            IsLoading = true;
            bool result = false;
            if (ResourceType.Id == 0)
                result = await MicroBus.Send(new CreateResourceTypeCommand(ResourceTypeUpdate)) > 0;
            else result = await MicroBus.Send(new UpdateResourceTypeCommand(ResourceType.Id,ResourceTypeUpdate));

            MHD.Notifications(ResourceType.Id == 0 ? ToastType.Add : ToastType.Update, result);
            await Callback.InvokeAsync(result);
        }

        private EditContext? editContext;
        private ValidationMessageStore? messageStore;

        protected override void OnInitialized()
        {
            editContext = new(ResourceTypeUpdate);
            editContext.OnValidationRequested += HandleValidationRequested;
            messageStore = new(editContext);
        }

        private void HandleValidationRequested(object sender, ValidationRequestedEventArgs args)
        {
            messageStore?.Clear();
            if (ResourceTypeUpdate.Type
            == ResourceTypesEnum.Materials && (ResourceTypeUpdate.CapWaste < 0) || ResourceTypeUpdate.CapWaste > 999)
            {
                messageStore?.Add(() => ResourceTypeUpdate.CapWaste, AppLoc[LocalizerConst.CapWasteValid, CalcResource.waste]);
            }
        }

        public void Dispose()
        {
            if (editContext is not null)
            {
                editContext.OnValidationRequested -= HandleValidationRequested;
            }
        }
    }
}
