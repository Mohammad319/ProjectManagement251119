using Application.Feature.Account.Commands;
using Application.Feature.Account.Queries;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ProjectManagement.Shared.DTO.Account;

namespace ProjectManagement.Components.ControlComponents.Accounts;

public partial class AccountsFormUI
{
    /// <summary>
    /// 0 = Create, >0 = Update
    /// </summary>
    [Parameter] public int Id { get; set; }

    /// <summary>
    /// Model passed from parent (for Update preload, for Create pass new()).
    /// </summary>
    [Parameter, EditorRequired] public required PostAccountDTO Model { get; set; }

    [Parameter] public EventCallback<bool> OnSaved { get; set; }

    [Inject] private DialogService DialogService { get; set; } = default!;
    [Inject] private ICommandDispatcher Dispatcher { get; set; } = default!;
    [Inject] private MhdServices MHD { get; set; } = default!;
    [Inject] private ILogger<AccountsFormUI> Logger { get; set; } = default!;

    private PostAccountDTO EditModel { get; set; } = new();
    private bool IsLoading { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        EditModel = new PostAccountDTO
        {
            Name = Model?.Name ?? string.Empty,
            Account = Model?.Account ?? string.Empty,
            AccountGroupId = Model?.AccountGroupId ?? 0,
            IsVisible = Model?.IsVisible ?? true,
            Data = Model?.Data ?? new AccountData()
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

            MHD.Notifications(Id == 0 ? ToastType.Add : ToastType.Update, ok);
            await OnSaved.InvokeAsync(ok);

            if (ok)
                CloseModal();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save Account. Id={Id}", Id);
            MHD.Notifications(ToastType.Danger, false);
            await OnSaved.InvokeAsync(false);
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }
    private void AddComment()
    {
        EditModel.Data ??= new AccountData();
        EditModel.Data.Comments ??= new List<string>();
        EditModel.Data.Comments.Add(string.Empty);
    }

    private void RemoveComment(int index)
    {
        if (EditModel?.Data?.Comments is null) return;
        if (index < 0 || index >= EditModel.Data.Comments.Count) return;

        EditModel.Data.Comments.RemoveAt(index);
    }

    private async Task<bool> CreateOrUpdateAsync(int id, PostAccountDTO dto)
    {
        if (id == 0)
            return await Dispatcher.Send(new CreateAccountCommand(dto)) > 0;

        return await Dispatcher.Send(new UpdateAccountCommand(dto, id));
    }
}
