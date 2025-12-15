using Application.Feature.Account.Commands;
using BlazorMHD.UI.Core.Services;
using Domain.Entities.Calculation;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Account;

namespace ProjectManagement.Components.ControlComponents.Accounts;

public partial class AccountGroupsFormUI
{
    [Parameter, EditorRequired] public required AccountGroupEntity AccountGroup { get; set; }
    [Parameter] public EventCallback<bool> OnSaved { get; set; }

    [Inject] private DialogService DialogService { get; set; } = default!;
    [Inject] private MhdServices Mhd { get; set; } = default!;
    [Inject] private ICommandDispatcher Dispatcher { get; set; } = default!;

    private PostAccountGroupDTO EditModel { get; set; } = new();
    private bool IsLoading { get; set; }

    protected override void OnParametersSet()
    {
        // إعادة تعبئة النموذج كل مرة تتغير فيها البيانات القادمة
        EditModel = new PostAccountGroupDTO();
        PropertyCopier.CopyPropertiesTo(AccountGroup, EditModel);
    }

    private void CloseModal() => DialogService.Close();

    private async Task HandleSubmitAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            var ok = await CreateOrUpdateAsync(AccountGroup.Id, EditModel);

            // إشعارات
            Mhd.Notifications(AccountGroup.Id == 0 ? ToastType.Add : ToastType.Update, ok);

            if (ok)
            {
                await OnSaved.InvokeAsync(true);
                CloseModal();
            }
            else
            {
                await OnSaved.InvokeAsync(false);
                // اترك المودال مفتوحًا ليصحح المستخدم
            }
        }
        catch
        {
            // خيار: Notification للخطأ العام
            Mhd.Notifications(ToastType.Danger, false);
            await OnSaved.InvokeAsync(false);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<bool> CreateOrUpdateAsync(int id, PostAccountGroupDTO dto)
    {
        if (id == 0)
            return await Dispatcher.Send(new CreateAccountGroupCommand(dto)) > 0;

        return await Dispatcher.Send(new UpdateAccountGroupCommand(dto, id));
    }
}
