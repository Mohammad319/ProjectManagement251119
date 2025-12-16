using Application.Feature.Account.Commands;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Account;

namespace ProjectManagement.Components.ControlComponents.Accounts;

public partial class AccountGroupsFormUI
{
    /// <summary>
    /// 0 = Create, >0 = Update
    /// </summary>
    [Parameter] public int Id { get; set; }

    /// <summary>
    /// Model used by the form. For Update: fill it before opening modal.
    /// For Create: pass new PostAccountGroupDTO().
    /// </summary>
    [Parameter, EditorRequired] public required PostAccountGroupDTO Model { get; set; }

    [Parameter] public EventCallback<bool> OnSaved { get; set; }

    [Inject] private DialogService DialogService { get; set; } = default!;
    [Inject] private MhdServices Mhd { get; set; } = default!;
    [Inject] private ICommandDispatcher Dispatcher { get; set; } = default!;
    [Inject] private ILogger<AccountGroupsFormUI> Logger { get; set; } = default!;

    // Bind this in Razor: Model="@EditModel"
    private PostAccountGroupDTO EditModel { get; set; } = new();

    private bool IsLoading { get; set; }

    protected override void OnParametersSet()
    {
        // Defensive copy: prevents editing the same DTO instance passed from parent.
        EditModel = new PostAccountGroupDTO
        {
            Name = Model?.Name ?? string.Empty
        };
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

            Mhd.Notifications(Id == 0 ? ToastType.Add : ToastType.Update, ok);

            await OnSaved.InvokeAsync(ok);

            if (ok)
                CloseModal();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save AccountGroup. Id={Id}", Id);
            Mhd.Notifications(ToastType.Danger, false);
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
