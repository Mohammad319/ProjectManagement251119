using Application.Feature.Project.ProcurementProcedure.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.ProcurementProcedure;

public partial class ProcurementProcedureFormUI
{
    [Parameter] public ProcurementProcedureEntity Procedure { get; set; } = new();
    [Parameter] public EventCallback<bool> Callback { get; set; }

    private PostTaskStatusDTO ProcedureUpdate { get; set; } = new();
    private bool IsLoading;
    private int? LastId;
    private ProcurementProcedureEntity? LastReference;

    protected override void OnParametersSet()
    {
        var currentId = Procedure?.Id;
        var sameReference = ReferenceEquals(LastReference, Procedure);
        if (sameReference && LastId == currentId)
            return;

        ProcedureUpdate = new PostTaskStatusDTO();
        PropertyCopier.CopyPropertiesTo(Procedure, ProcedureUpdate);

        if (string.IsNullOrWhiteSpace(ProcedureUpdate.Color))
            ProcedureUpdate.Color = "#3b82f6";

        LastId = currentId;
        LastReference = Procedure;
    }

    private void CloseModal() => MHD.Modal.CloseAsync();

    private async Task HandleSubmitAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            bool result = Procedure.Id == 0
                ? await Dispatcher.Send(new CreateProcurementProcedureCommand(ProcedureUpdate)) > 0
                : await Dispatcher.Send(new UpdateProcurementProcedureCommand(Procedure.Id, ProcedureUpdate));

            MHD.Notifications(Procedure.Id == 0 ? ToastType.Add : ToastType.Update, result);
            await Callback.InvokeAsync(result);
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}
