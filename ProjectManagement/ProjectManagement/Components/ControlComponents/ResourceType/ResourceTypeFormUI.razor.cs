using Application.Feature.Account.Queries;
using Application.Feature.Calculation.ResourceType.Commands;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Components.ControlComponents.Department;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.ResourceType;
using ProjectManagement.Shared.Enums;
namespace ProjectManagement.Components.ControlComponents.ResourceType;

public partial class ResourceTypeFormUI
{
    public const string DialogFormId = "resourceTypeForm";
    [Parameter] public EventCallback<bool> Callback { get; set; }
    [Parameter] public ResourceTypeModel ResourceType { get; set; } = new();

    private readonly PostResourceTypeDTO ResourceTypeUpdate = new();
    private bool IsLoading;
    private List<ListAccountGroupIncludeAccountDTO>? AccountGroups;
    private EditContext? editContext;
    private ResourceTypeModel? loadedResourceType;
    private int loadedResourceTypeId = -1;

    private static readonly IReadOnlyList<AppSelect<ResourceTypesEnum>.Option> ResourceTypeOptions =
        Enum.GetValues<ResourceTypesEnum>()
            .Select(level => new AppSelect<ResourceTypesEnum>.Option(level, ResourceLocalize.GetResourceType(level)))
            .ToList();

    // Standardkonto options are limited to the accounts the admin has marked as allowed.
    private IReadOnlyList<AppSelect<int?>.Option> AllowedAccountOptions =>
        (AccountGroups ?? [])
            .SelectMany(g => g.Accounts ?? [])
            .Where(a => ResourceTypeUpdate.AllowedAccountIds.Contains(a.Id))
            .Select(a => new AppSelect<int?>.Option(
                a.Id,
                string.IsNullOrWhiteSpace(a.Account) ? a.Name : $"{a.Account} ({a.Name})"))
            .ToList();

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
    }

    private void OnAllowedAccountsChanged(List<int> allowed)
    {
        ResourceTypeUpdate.AllowedAccountIds = allowed;

        // A default account must remain within the allowed set.
        if (ResourceTypeUpdate.AccountId is > 0 && !allowed.Contains(ResourceTypeUpdate.AccountId.Value))
            ResourceTypeUpdate.AccountId = null;
    }

    private void CloseModal() => MHD.Modal.CloseAsync();

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
