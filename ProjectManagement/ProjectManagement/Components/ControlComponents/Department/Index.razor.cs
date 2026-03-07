using Domain.DTO.User;
using BlazorMHD.UI.Core.Services;
using ProjectManagement.Client.Services.MHDBlazor;
using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Shared;
using Application.Feature.Identity.Department.Commands;
using Application.Feature.Identity.Department.Queries;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Identity;
using ProjectManagement.Shared.Models.Account;

namespace ProjectManagement.Components.ControlComponents.Department;

public partial class Index
{
    private enum ViewMode
    {
        List = 0,
        Details = 1,
        Users = 2
    }

    private List<DepartmentDetailsDTO>? Departments;
    private DepartmentDetailsDTO? SelectedDepartment;
    private int? SelectedDepartmentIdForUsers;
    private ViewMode Mode = ViewMode.List;

    protected override async Task OnInitializedAsync()
        => await ReloadDepartmentsAsync();

    private async Task ReloadDepartmentsAsync()
    {
        Departments = await Dispatcher.Send(new GetDepartmentsQuery());
        await InvokeAsync(StateHasChanged);
    }

    private void BackToList()
    {
        SelectedDepartment = null;
        SelectedDepartmentIdForUsers = 0;
        Mode = ViewMode.List;

        MHD.Modal.Close();
        _ = InvokeAsync(StateHasChanged);
    }

    private void OpenRegisterUser()
    {
        var user = new TenantUserDto();

        MHD.Modal.ShowComponent<UpdateUserUI>(
            PMResourceIdentity.register,
            new Dictionary<string, object>
            {
                [nameof(UpdateUserUI.UserForm)] = user,
                [nameof(UpdateUserUI.Callback)] = EventCallback.Factory.Create<bool>(this, OnModalResultAsync),
            },
            DialogSize.ExtraLarge);
    }

    private void OpenCreateDepartment()
        => OpenDepartmentModal(new DepartmentDetailsDTO { Id = 0 });

    private void OpenEditDepartment(DepartmentDetailsDTO department)
        => OpenDepartmentModal(department);

    private void OpenDepartmentModal(DepartmentDetailsDTO department)
    {
        var departmentText = AppLoc[nameof(ResourceApp.department)];

        var title = department.Id > 0
            ? AppLoc[LocalizerConst.Update, department.Name]
            : AppLoc[LocalizerConst.New, departmentText];

        MHD.Modal.ShowComponent<CreateDepartment>(
            title,
            new Dictionary<string, object>
            {
                [nameof(CreateDepartment.DepartmentList)] = department,
                [nameof(CreateDepartment.OnClickCallback)] = EventCallback.Factory.Create<bool>(this, OnModalResultAsync),
            },
            DialogSize.ExtraLarge);
    }

    private void OpenAdminUsers()
    {
        SelectedDepartment = null;
        SelectedDepartmentIdForUsers = null;
        Mode = ViewMode.Users;
    }

    private void OpenDepartmentUsers(int departmentId)
    {
        SelectedDepartment = Departments?.FirstOrDefault(x => x.Id == departmentId);
        SelectedDepartmentIdForUsers = departmentId;
        Mode = ViewMode.Users;
    }

    private void AskRemoveDepartment(DepartmentDetailsDTO department)
    {
        MHD.DeleteMessage(
            department.Name,
            EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(department)));
    }

    private async Task ConfirmRemoveAsync(DepartmentDetailsDTO department)
    {
        var ok = await Dispatcher.Send(new DeleteDepartmentCommand(department.Id));

        MHD.Notifications(ToastType.Delete, ok);

        if (ok)
            await ReloadDepartmentsAsync();

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnModalResultAsync(bool isSuccess)
    {
        if (isSuccess)
            await ReloadDepartmentsAsync();

        BackToList();
    }
}