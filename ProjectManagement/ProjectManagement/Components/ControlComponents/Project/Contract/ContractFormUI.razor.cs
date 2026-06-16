using Application.Feature.Project.Contract.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.Contract;

public partial class ContractFormUI
{
    [Parameter] public ContractEntity Contract { get; set; } = new();
    [Parameter] public EventCallback<bool> Callback { get; set; }

    private PostTaskStatusDTO UpdateObj { get; set; } = new();
    private bool IsLoading;
    private int? LastContractId;
    private ContractEntity? LastContractReference;

    protected override void OnParametersSet()
    {
        var currentId = Contract?.Id;
        var sameReference = ReferenceEquals(LastContractReference, Contract);
        if (sameReference && LastContractId == currentId)
            return;

        UpdateObj = new PostTaskStatusDTO();
        PropertyCopier.CopyPropertiesTo(Contract, UpdateObj);

        if (string.IsNullOrWhiteSpace(UpdateObj.Color))
            UpdateObj.Color = "#0ea5e9";

        LastContractId = currentId;
        LastContractReference = Contract;
    }

    private void CloseModal() => MHD.Modal.CloseAsync();

    private async Task HandleSubmitAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            bool result = Contract.Id == 0
                ? await Dispatcher.Send(new CreateContractCommand(UpdateObj)) > 0
                : await Dispatcher.Send(new UpdateContractCommand(Contract.Id, UpdateObj));

            MHD.Notifications(Contract.Id == 0 ? ToastType.Add : ToastType.Update, result);
            await Callback.InvokeAsync(result);
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}
