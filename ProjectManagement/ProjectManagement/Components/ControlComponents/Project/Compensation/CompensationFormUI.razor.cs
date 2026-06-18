using Application.Feature.Project.ProcurementMethods.Commands;
using Domain.Entities.Project;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Components.ControlComponents.Project.Compensation;

public partial class CompensationFormUI
{
    [Parameter] public CompensationEntity Compensation { get; set; } = new();
    [Parameter] public EventCallback<bool> Callback { get; set; }

    private PostTaskStatusDTO CompensationUpdate { get; set; } = new();
    private bool IsLoading;
    private int? LastCompensationId;
    private CompensationEntity? LastCompensationReference;

    protected override void OnParametersSet()
    {
        var currentId = Compensation?.Id;
        var sameReference = ReferenceEquals(LastCompensationReference, Compensation);
        if (sameReference && LastCompensationId == currentId)
            return;

        CompensationUpdate = new PostTaskStatusDTO();
        PropertyCopier.CopyPropertiesTo(Compensation, CompensationUpdate);

        LastCompensationId = currentId;
        LastCompensationReference = Compensation;
    }

    private void CloseModal() => MHD.Modal.CloseAsync();

    private async Task HandleSubmitAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            bool result = Compensation.Id == 0
                ? await Dispatcher.Send(new CreateCompensationCommand(CompensationUpdate)) > 0
                : await Dispatcher.Send(new UpdateCompensationCommand(Compensation.Id, CompensationUpdate));

            MHD.Notifications(Compensation.Id == 0 ? ToastType.Add : ToastType.Update, result);
            await Callback.InvokeAsync(result);
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}
