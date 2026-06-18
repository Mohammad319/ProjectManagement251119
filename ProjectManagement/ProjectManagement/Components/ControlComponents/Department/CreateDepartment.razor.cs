using ProjectManagement.Client.Services.MHDBlazor;
using ProjectManagement.Shared;
using Application.Feature.Identity.Department.Commands;
using Application.Feature.Identity.Department.Queries;
using Domain.DTO.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using ProjectManagement.Shared.Base.Users;
using ProjectManagement.Shared.DTO.Identity;

namespace ProjectManagement.Components.ControlComponents.Department;

using ProjectManagement.Components.Shared;

public partial class CreateDepartment : AppComponentBase
{
    [Parameter] public EventCallback<bool> OnClickCallback { get; set; }
    [Parameter] public DepartmentDetailsDTO? DepartmentList { get; set; }
    [Parameter] public IReadOnlyCollection<string>? ExistingNames { get; set; }

    [Inject] private IStringLocalizer<PMWebResource> WebLoc { get; set; } = default!;

    protected DepartmentBase? departmentPost;
    protected EditContext? editContext;
    protected bool IsSaving;

    // Users of the department being edited, used to pick a department head.
    protected List<TenantUserDto>? DepartmentUsers;

    private int _loadedId;
    private string? _loadedName;
    private string? _loadedDesc;

    protected override async Task OnParametersSetAsync()
    {
        var id = DepartmentList?.Id ?? 0;
        var name = DepartmentList?.Name ?? string.Empty;
        var desc = DepartmentList?.Description;

        if (editContext is not null &&
            _loadedId == id &&
            _loadedName == name &&
            _loadedDesc == desc)
        {
            return;
        }

        _loadedId = id;
        _loadedName = name;
        _loadedDesc = desc;

        departmentPost = new DepartmentBase
        {
            Name = name,
            Description = desc ?? string.Empty,
            Color = DepartmentList?.Color,
            HeadUserId = DepartmentList?.HeadUserId
        };

        editContext = new EditContext(departmentPost);

        // The head can only be one of the department's own users, available when editing.
        DepartmentUsers = id > 0
            ? await Dispatcher.Send(new GetUserssQuery(id))
            : null;
    }

    protected string HeadUserDisplay(TenantUserDto user)
    {
        var fullName = string.Join(' ', new[] { user.Firstname, user.Lastname }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(fullName) ? user.Email ?? user.Username ?? "—" : fullName;
    }

    protected async Task Close()
    {
        if (OnClickCallback.HasDelegate)
            await OnClickCallback.InvokeAsync(false);
    }

    protected async Task HandleSubmitAsync()
    {
        if (departmentPost is null || IsSaving)
        {
            if (departmentPost is null && OnClickCallback.HasDelegate)
                await OnClickCallback.InvokeAsync(false);
            return;
        }

        IsSaving = true;
        try
        {
            departmentPost.Name = (departmentPost.Name ?? string.Empty).Trim();
            departmentPost.Description = (departmentPost.Description ?? string.Empty).Trim();

            if (ExistingNames is not null
                && ExistingNames.Any(n => string.Equals(n?.Trim(), departmentPost.Name, StringComparison.OrdinalIgnoreCase)))
            {
                MHD.MessageOk(WebLoc["DuplicateDepartmentTitle"].Value, WebLoc["DuplicateDepartmentMessage"].Value);
                return;
            }

            if ((DepartmentList?.Id ?? 0) > 0)
            {
                var ok = await Dispatcher.Send(new UpdateDepartmentCommand(DepartmentList!.Id, departmentPost));
                MHD.Notifications(ToastType.Update, ok);

                if (OnClickCallback.HasDelegate)
                    await OnClickCallback.InvokeAsync(ok);

                return;
            }

            var newId = await Dispatcher.Send(new CreateDepartmentCommand(departmentPost));
            var created = newId > 0;
            MHD.Notifications(ToastType.Add, created);

            if (OnClickCallback.HasDelegate)
                await OnClickCallback.InvokeAsync(created);
        }
        finally
        {
            IsSaving = false;
        }
    }
}