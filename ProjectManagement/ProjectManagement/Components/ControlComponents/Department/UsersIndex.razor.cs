using Application.Feature.Identity.Department.Queries;
using AuthPermissions.Context;
using BlazorMHD.UI.Core.Services;
using DocumentFormat.OpenXml.Spreadsheet;
using Domain.DTO.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;
using ProjectManagement.Services;

namespace ProjectManagement.Components.ControlComponents.Department
{

    public partial class UsersIndex
    {
        [Parameter] public int? DepartmentId { get; set; } = null;
        [Parameter] public EventCallback<bool> OnClickCallback { get; set; }
        [Inject] public ICommandDispatcher MicroBus { get; set; } = default!;
        [Inject] public ITenantUserService TenantUserService { get; set; } = default!;
        [Inject] public ShardingSingleDbContext shContext { get; set; } = default!;
        [Inject] public UserManager<ApplicationUser> UserManager { get; set; } = default!;

        // UI State
        protected List<TenantUserDto>? Users { get; set; }
        protected bool IsLoading { get; set; } = true;
        protected bool IsBusy { get; set; } = false;

        protected override async Task OnParametersSetAsync()
        {
            // إذا تغيّر DepartmentId أثناء نفس عمر الكمبوننت (تنقل/فتح جديد)
            await LoadAsync();
        }

        protected Task Back() => OnClickCallback.InvokeAsync(false);

        protected async Task LoadAsync()
        {
            try
            {
                IsLoading = true;
                StateHasChanged();
                Users = await MicroBus.Send(new GetUserssQuery(DepartmentId));
                //
                // أو:
                // Users = await TenantUserService.GetUsersByDepartmentAsync(DepartmentId);

                Users ??= []; // placeholder آمن لحين ربط الـ query الحقيقي
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
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
                StateHasChanged();
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
                // استدعِ دالتك الحالية إن كانت موجودة:
                // await Delete_Click(user);
                //
                // أو ضع منطق الحذف هنا (MicroBus / Service ...)

                await Task.CompletedTask;

                await LoadAsync();
            }
            finally
            {
                IsBusy = false;
                StateHasChanged();
            }
        }

        protected async Task ConfirmRemoveOnlyFromRegisterAsync(TenantUserDto user)
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // استدعِ دالتك الحالية إن كانت موجودة:
                // await DeleteOnlyFRomRegister_Click(user);
                //
                // أو ضع منطق الإزالة هنا

                await Task.CompletedTask;

                await LoadAsync();
            }
            finally
            {
                IsBusy = false;
                StateHasChanged();
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
                await LoadAsync();   // إعادة تحميل المستخدمين

            StateHasChanged();
        }
    }
}
