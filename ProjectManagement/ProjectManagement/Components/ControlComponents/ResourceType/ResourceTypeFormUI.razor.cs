using Application.Feature.Account.Queries;
using Application.Feature.Calculation.ResourceType.Commands;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.ResourceType;
namespace ProjectManagement.Components.ControlComponents.ResourceType;

public partial class ResourceTypeFormUI
{
    public const string DialogFormId = "resourceTypeForm";
    [Parameter] public EventCallback<bool> Callback { get; set; }
    [Parameter] public ResourceTypeModel ResourceType { get; set; } = new();

    private readonly PostResourceTypeDTO ResourceTypeUpdate = new();
    private bool IsLoading;
    private List<ListAccountGroupIncludeAccountDTO>? AccountGroups;
    private ListAccountGroupIncludeAccountDTO? AccountGroupSelected;
    private EditContext? editContext;
    private ResourceTypeModel? loadedResourceType;
    private int loadedResourceTypeId = -1;

    protected override void OnInitialized()
    {
        editContext = new EditContext(ResourceTypeUpdate);
    }

    protected override async Task OnParametersSetAsync()
    {
        AccountGroups ??= await Dispatcher.Send(new GetAccountGroupsAsListQuery());

        var shouldSyncFromParameters =
            loadedResourceType is null ||
            !ReferenceEquals(ResourceType, loadedResourceType) ||
            ResourceType.Id != loadedResourceTypeId;

        if (shouldSyncFromParameters)
        {
            PropertyCopier.CopyPropertiesTo(ResourceType, ResourceTypeUpdate);
            loadedResourceType = ResourceType;
            loadedResourceTypeId = ResourceType.Id;
        }

        if (ResourceType.Id > 0)
        {
            AccountGroupSelected = AccountGroups?.FirstOrDefault(x => x.Accounts.Any(a => a.Id == ResourceTypeUpdate.AccountId));
        }
        else if (ResourceTypeUpdate.AccountId is null or 0)
        {
            AccountGroupSelected = null;
        }
    }

    private async Task ChangeGroupAccount(ChangeEventArgs e)
    {
        AccountGroupSelected = null;
        ResourceTypeUpdate.AccountId = null;

        if (!int.TryParse(e.Value?.ToString(), out var id) || id == 0)
            return;

        await Task.Yield();
        AccountGroupSelected = AccountGroups?.FirstOrDefault(x => x.Id == id);
    }

    private void CloseModal() => MHD.Modal.Close();

    private async Task HandleSubmitAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            bool result;

            if (ResourceType.Id == 0)
                result = await Dispatcher.Send(new CreateResourceTypeCommand(ResourceTypeUpdate)) > 0;
            else
                result = await Dispatcher.Send(new UpdateResourceTypeCommand(ResourceType.Id, ResourceTypeUpdate));

            MHD.Notifications(ResourceType.Id == 0 ? ToastType.Add : ToastType.Update, result);
            await Callback.InvokeAsync(result);
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

}
