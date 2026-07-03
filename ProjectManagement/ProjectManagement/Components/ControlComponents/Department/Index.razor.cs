using Domain.DTO.User;
using BlazorMHD.UI.Core.Services;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Services.MHDBlazor;
using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Shared;
using Application.Feature.Identity.Department.Commands;
using Application.Feature.Identity.Department.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ProjectManagement.Shared.DTO.Identity;
using ProjectManagement.Shared.Models.Account;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles;

namespace ProjectManagement.Components.ControlComponents.Department;

public partial class Index
{
    private enum ViewMode
    {
        List = 0,
        Details = 1,
        Users = 2
    }

    protected enum DeptSortColumn { Name, Users, Projects }

    private const int PageSize = 10;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private List<DepartmentDetailsDTO>? Departments;
    private DepartmentDetailsDTO? SelectedDepartment;
    private int? SelectedDepartmentIdForUsers;
    private bool WithoutDepartmentOnly;
    private ViewMode Mode = ViewMode.List;

    // Filter / sort / paging state
    private string _departmentSearch = string.Empty;
    private List<DepartmentDetailsDTO>? _filteredCache;

    protected DeptSortColumn SortColumn { get; private set; } = DeptSortColumn.Name;
    protected bool SortDescending { get; private set; }
    protected int CurrentPage { get; private set; } = 1;

    protected string DepartmentSearch
    {
        get => _departmentSearch;
        set
        {
            if (_departmentSearch == value)
                return;

            _departmentSearch = value;
            CurrentPage = 1;
            InvalidateFilterCache();
        }
    }

    private void InvalidateFilterCache() => _filteredCache = null;

    protected IReadOnlyList<DepartmentDetailsDTO> FilteredDepartments
        => _filteredCache ??= ComputeFiltered();

    private List<DepartmentDetailsDTO> ComputeFiltered()
    {
        IEnumerable<DepartmentDetailsDTO> all = Departments ?? Enumerable.Empty<DepartmentDetailsDTO>();

        if (!string.IsNullOrWhiteSpace(DepartmentSearch))
        {
            var term = DepartmentSearch.Trim();
            all = all.Where(d =>
                (!string.IsNullOrEmpty(d.Name) && d.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrEmpty(d.Description) && d.Description.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        Func<DepartmentDetailsDTO, IComparable> key = SortColumn switch
        {
            DeptSortColumn.Users => d => d.UsersCount,
            DeptSortColumn.Projects => d => d.ProjectsCount,
            _ => d => d.Name ?? string.Empty
        };

        all = SortDescending ? all.OrderByDescending(key) : all.OrderBy(key);
        return all.ToList();
    }

    protected int TotalPages
        => Math.Max(1, (int)Math.Ceiling(FilteredDepartments.Count / (double)PageSize));

    protected IReadOnlyList<DepartmentDetailsDTO> PagedDepartments
    {
        get
        {
            var page = Math.Min(Math.Max(CurrentPage, 1), TotalPages);
            return FilteredDepartments.Skip((page - 1) * PageSize).Take(PageSize).ToList();
        }
    }

    protected void SortBy(DeptSortColumn column)
    {
        if (SortColumn == column)
            SortDescending = !SortDescending;
        else
        {
            SortColumn = column;
            SortDescending = false;
        }

        CurrentPage = 1;
        InvalidateFilterCache();
    }

    protected void GoToPage(int page)
    {
        CurrentPage = Math.Min(Math.Max(page, 1), TotalPages);
    }

    protected int TotalUsersCount { get; private set; }
    protected int TotalProjectsCount => Departments?.Sum(d => d.ProjectsCount) ?? 0;
    protected bool IsLoadingDepartments { get; private set; }
    protected string? DepartmentLoadError { get; private set; }

    protected override async Task OnInitializedAsync()
        => await ReloadDepartmentsAsync();

    private async Task ReloadDepartmentsAsync()
    {
        try
        {
            IsLoadingDepartments = true;
            DepartmentLoadError = null;
            Departments = await Dispatcher.Send(new GetDepartmentsQuery());
            TotalUsersCount = await Dispatcher.Send(new GetTenantUsersCountQuery());
        }
        catch (Exception ex)
        {
            // Don't let a load failure escape the lifecycle method and kill the circuit
            // (endless reconnect / blank screen). Show it and keep the list usable.
            Departments ??= [];
            DepartmentLoadError = ex.Message;
        }
        finally
        {
            IsLoadingDepartments = false;
            InvalidateFilterCache();
            await InvokeAsync(StateHasChanged);
        }
    }

    protected async Task ExportDepartmentsAsync()
    {
        var rows = FilteredDepartments.Select(d => new object?[]
        {
            d.Name,
            d.Description,
            d.UsersCount,
            d.ProjectsCount,
            d.FoldersCount
        });

        var columns = new[]
        {
            CalcResource.name,
            AppLoc[nameof(ResourceLoc.description)].Value,
            WebLoc["UsersLabel"].Value,
            CalcResource.project,
            WebLoc["FoldersLabel"].Value
        };

        await ReportExportInterop.ExportExcelAsync(JS, "departments", WebLoc["DepartmentsTitle"].Value, columns, rows);
    }

    // Only resets the in-page view. It must NOT close the modal host: "Tillbaka till
    // avdelningar" would otherwise close the whole settings window.
    private void BackToList()
    {
        SelectedDepartment = null;
        SelectedDepartmentIdForUsers = null;
        WithoutDepartmentOnly = false;
        Mode = ViewMode.List;

        _ = InvokeAsync(StateHasChanged);
    }

    private void OpenAuditLog()
        => MHD.Modal.ShowComponent<UserAuditLogUI>(
            "Användaraktivitetslogg",
            new Dictionary<string, object>(),
            MhdDialogSize.ExtraLarge);

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
            MhdDialogSize.ExtraLarge,
            DialogButtonsHelper.CreateSaveCancelButtons(UpdateUserUI.DialogFormId));
    }

    private void OpenCreateDepartment()
        => OpenDepartmentModal(new DepartmentDetailsDTO { Id = 0 });

    private void OpenEditDepartment(DepartmentDetailsDTO department)
        => OpenDepartmentModal(department);

    private void OpenDepartmentDetails(DepartmentDetailsDTO department)
    {
        SelectedDepartment = department;
        Mode = ViewMode.Details;
    }

    private void OpenDepartmentModal(DepartmentDetailsDTO department)
    {
        var title = department.Id > 0
            ? AppLoc[LocalizerConst.Update, department.Name].Value
            : WebLoc["NewDepartment"].Value;

        var existingNames = (Departments ?? Enumerable.Empty<DepartmentDetailsDTO>())
            .Where(d => d.Id != department.Id)
            .Select(d => d.Name)
            .ToList();

        MHD.Modal.ShowComponent<CreateDepartment>(
            title,
            new Dictionary<string, object>
            {
                [nameof(CreateDepartment.DepartmentList)] = department,
                [nameof(CreateDepartment.ExistingNames)] = existingNames,
                [nameof(CreateDepartment.OnClickCallback)] = EventCallback.Factory.Create<bool>(this, OnModalResultAsync),
            },
            MhdDialogSize.ExtraLarge);
    }

    private void OpenAllUsers()
    {
        SelectedDepartment = null;
        SelectedDepartmentIdForUsers = null;
        WithoutDepartmentOnly = false;
        Mode = ViewMode.Users;
    }

    private void OpenUsersWithoutDepartment()
    {
        SelectedDepartment = null;
        SelectedDepartmentIdForUsers = null;
        WithoutDepartmentOnly = true;
        Mode = ViewMode.Users;
    }

    private void OpenDepartmentUsers(int departmentId)
    {
        SelectedDepartment = Departments?.FirstOrDefault(x => x.Id == departmentId);
        SelectedDepartmentIdForUsers = departmentId;
        WithoutDepartmentOnly = false;
        Mode = ViewMode.Users;
    }

    private void AskRemoveDepartment(DepartmentDetailsDTO department)
    {
        // The backend refuses to delete a department that still owns users/folders/projects.
        // Surface the reason up front instead of letting the delete silently fail.
        if (department.UsersCount > 0 || department.ProjectsCount > 0 || department.FoldersCount > 0)
        {
            var message = string.Format(
                WebLoc["DepartmentInUseMessageFormat"].Value,
                department.Name,
                department.UsersCount,
                department.ProjectsCount,
                department.FoldersCount);

            MHD.MessageOk(WebLoc["DepartmentInUseTitle"].Value, message);
            return;
        }

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
        await MHD.Modal.CloseAsync();

        if (isSuccess)
            await ReloadDepartmentsAsync();

        BackToList();
    }
}
