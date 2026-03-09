using Application.Feature.Account.Commands;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Account;

namespace ProjectManagement.Components.ControlComponents.Accounts;

public partial class AccountGroupsFormUI
{
    [Parameter] public int Id { get; set; }
    [Parameter, EditorRequired] public required PostAccountGroupDTO Model { get; set; }
    [Parameter] public EventCallback<bool> OnSaved { get; set; }

    [Inject] private ILogger<AccountGroupsFormUI> Logger { get; set; } = default!;

    private PostAccountGroupDTO EditModel { get; set; } = new();
    private bool IsLoading { get; set; }
    private int LastId = -1;
    private PostAccountGroupDTO? LastModelReference;

    protected override void OnParametersSet()
    {
        var sameReference = ReferenceEquals(LastModelReference, Model);
        if (sameReference && LastId == Id)
            return;

        EditModel = new PostAccountGroupDTO
        {
            Name = Model?.Name ?? string.Empty
        };

        LastId = Id;
        LastModelReference = Model;
    }

    private void CloseModal() => DialogService.Close();

    private async Task HandleSubmitAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var ok = await CreateOrUpdateAsync(Id, EditModel);

            MHD.Notifications(Id == 0 ? ToastType.Add : ToastType.Update, ok);

            await OnSaved.InvokeAsync(ok);

            if (ok)
                CloseModal();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save AccountGroup. Id={Id}", Id);
            MHD.Notifications(ToastType.Danger, false);
            await OnSaved.InvokeAsync(false);
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task<bool> CreateOrUpdateAsync(int id, PostAccountGroupDTO dto)
    {
        if (id == 0)
            return await Dispatcher.Send(new CreateAccountGroupCommand(dto)) > 0;

        return await Dispatcher.Send(new UpdateAccountGroupCommand(dto, id));
    }
}
