using Application.Feature.Identity.Department.Queries;
using BlazorMHD.UI.Core.Services;
using Domain.DTO.User;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Services;

namespace ProjectManagement.Components.ControlComponents.Department
{
    public partial class UsersIndex
    {
        [Parameter] public int? DepartmentId { get; set; } = null;
        [Parameter] public EventCallback<bool> OnClickCallback { get; set; }

        [Inject] public ITenantUserService TenantUserService { get; set; } = default!;

        // UI State
        protected List<TenantUserDto>? Users { get; set; }
        protected bool IsLoading { get; set; } = true;
        protected bool IsBusy { get; set; } = false;

        // منع Reload إذا لم يتغير DepartmentId (تحسين بسيط للأداء)
        private int? _lastDepartmentId;

        protected override async Task OnParametersSetAsync()
        {
            if (_lastDepartmentId == DepartmentId && Users is not null)
                return;

            _lastDepartmentId = DepartmentId;
            await LoadAsync();
        }

        protected async Task Back()
        {
            if (OnClickCallback.HasDelegate)
                await OnClickCallback.InvokeAsync(false);
        }

        protected async Task LoadAsync()
        {
            try
            {
                IsLoading = true;
                await InvokeAsync(StateHasChanged);

                Users = await Dispatcher.Send(new GetUserssQuery(DepartmentId));
                Users ??= [];
            }
            finally
            {
                IsLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task RecreateUserAsync(TenantUserDto user)
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                await TenantUserService.RecreateUserAsync(user);
                await LoadAsync();
            }
            finally
            {
                IsBusy = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void AskDeleteUser(TenantUserDto user)
        {
            var label = user.Email ?? user.Username ?? "User";
            MHD.DeleteMessage(label, EventCallback.Factory.Create(this, () => ConfirmDeleteAsync(user)));
        }

        protected void AskRemoveOnlyFromRegister(TenantUserDto user)
        {
            var label = user.Email ?? user.Username ?? "User";
            MHD.DeleteMessage(label, EventCallback.Factory.Create(this, () => ConfirmRemoveOnlyFromRegisterAsync(user)));
        }

        protected async Task ConfirmDeleteAsync(TenantUserDto user)
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // ضع منطق الحذف الفعلي عندك هنا (MicroBus أو Service)
                await Task.CompletedTask;
                await LoadAsync();
            }
            finally
            {
                IsBusy = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task ConfirmRemoveOnlyFromRegisterAsync(TenantUserDto user)
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // ضع منطق الإزالة الفعلي عندك هنا
                await Task.CompletedTask;
                await LoadAsync();
            }
            finally
            {
                IsBusy = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void EditUser(TenantUserDto user)
        {
            if (IsBusy) return;

            MHD.Modal.ShowComponent<UpdateUserUI>(
                AppLoc[LocalizerConst.Update, user.Email ?? user.Username],
                new Dictionary<string, object>
                {
                    [nameof(UpdateUserUI.UserForm)] = user,
                    [nameof(UpdateUserUI.DepartmentId)] = DepartmentId,
                    [nameof(UpdateUserUI.Callback)] =
                        EventCallback.Factory.Create<bool>(this, OnEditUserResultAsync)
                },
                DialogSize.ExtraLarge
            );
        }

        private async Task OnEditUserResultAsync(bool isSuccess)
        {
            MHD.Modal.Close();

            if (isSuccess)
                await LoadAsync();

            await InvokeAsync(StateHasChanged);
        }
    }
}
