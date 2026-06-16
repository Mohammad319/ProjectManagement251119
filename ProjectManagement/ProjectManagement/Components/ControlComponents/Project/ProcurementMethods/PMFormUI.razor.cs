using Application.Feature.Project.ProcurementMethods.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.ProcurementMethods;

public partial class PMFormUI
{
    [Parameter] public ProcurementMethodEntity Procurement { get; set; } = new();
    [Parameter] public EventCallback<bool> Callback { get; set; }

    private PostTaskStatusDTO ProcurementUpdate { get; set; } = new();
    private bool IsLoading;
    private int? LastProcurementId;
    private ProcurementMethodEntity? LastProcurementReference;

    protected override void OnParametersSet()
    {
        var currentId = Procurement?.Id;
        var sameReference = ReferenceEquals(LastProcurementReference, Procurement);
        if (sameReference && LastProcurementId == currentId)
            return;

        ProcurementUpdate = new PostTaskStatusDTO();
        PropertyCopier.CopyPropertiesTo(Procurement, ProcurementUpdate);

        if (string.IsNullOrWhiteSpace(ProcurementUpdate.Color))
            ProcurementUpdate.Color = "#3b82f6";

        LastProcurementId = currentId;
        LastProcurementReference = Procurement;
    }

    private void CloseModal() => MHD.Modal.CloseAsync();

    private async Task HandleSubmitAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            bool result = Procurement.Id == 0
                ? await Dispatcher.Send(new CreateProcurementMethodCommand(ProcurementUpdate)) > 0
                : await Dispatcher.Send(new UpdateProcurementMethodCommand(Procurement.Id, ProcurementUpdate));

            MHD.Notifications(Procurement.Id == 0 ? ToastType.Add : ToastType.Update, result);
            await Callback.InvokeAsync(result);
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}
