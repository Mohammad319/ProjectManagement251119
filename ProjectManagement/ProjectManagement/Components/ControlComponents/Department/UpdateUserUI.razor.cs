using ProjectManagement.Client.Services.MHDBlazor;
using ProjectManagement.Shared;
using ProjectManagement.Shared.Constant;
using Application.Feature.Identity.Department.Queries;
using Domain.DTO.User;
using Domain.Entities.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using ProjectManagement.Services;
using ProjectManagement.Shared.DTO.General;

namespace ProjectManagement.Components.ControlComponents.Department;

using AuthPermissions.Context;
using ProjectManagement.Client.Shared.SharedComponent;
using ProjectManagement.Components.Shared;

public partial class UpdateUserUI : AppComponentBase
{
    public const string DialogFormId = "userForm";
    [Parameter] public EventCallback<bool> Callback { get; set; }
    [Parameter] public TenantUserDto UserForm { get; set; } = default!;
    [Parameter] public int? DepartmentId { get; set; }

    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private ITenantUserService TenantUserService { get; set; } = default!;
    [Inject] private IStringLocalizer<PMWebResource> WebLoc { get; set; } = default!;

    protected TenantUserDto? Form { get; set; }
    protected List<ListDTO>? Departments { get; set; }

    protected bool IsLoading { get; set; } = true;
    protected bool IsSaving { get; set; }
    protected bool IsBusy => IsLoading || IsSaving;

    /// <summary>Selected departments in click order; the first one becomes the user's primary department.</summary>
    protected List<int> SelectedDepartmentIds { get; } = [];
    protected int? PrimaryDepartmentId => SelectedDepartmentIds.Count > 0 ? SelectedDepartmentIds[0] : null;

    /// <summary>"Tills vidare": lockout without an end date.</summary>
    protected bool LockIndefinitely { get; set; }

    protected List<string> ValidationErrors { get; } = [];
    private readonly Dictionary<string, string> _fieldErrors = new();

    protected string? FieldError(string field)
        => _fieldErrors.TryGetValue(field, out var message) ? message : null;

    protected IEnumerable<MhdSelectItem<string>> RoleOptions =>
    [
        new MhdSelectItem<string> { Value = PMRolesConst.Tenant.Admin, Label = WebLoc["LevelManager"].Value },
        new MhdSelectItem<string> { Value = PMRolesConst.Tenant.Manger, Label = WebLoc["LevelUser"].Value },
        new MhdSelectItem<string> { Value = PMRolesConst.Tenant.User, Label = WebLoc["LevelGuest"].Value }
    ];

    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;

        try
        {
            if (DepartmentId == 0)
                DepartmentId = null;

            Departments = await Dispatcher.Send(new GetDepartmentsAsListQuery());

            var sourceUser = UserForm ?? new TenantUserDto();
            Form = new TenantUserDto
            {
                Email = sourceUser.Email,
                Firstname = sourceUser.Firstname,
                Lastname = sourceUser.Lastname,
                DepartmentId = DepartmentId ?? sourceUser.DepartmentId,
                DepartmentIds = sourceUser.DepartmentIds.ToList(),
                LockoutEnabled = sourceUser.LockoutEnabled,
                IdAuth = sourceUser.IdAuth,
                LockoutStart = sourceUser.LockoutStart,
                LockoutEnd = sourceUser.LockoutEnd,
                PhoneNumber = sourceUser.PhoneNumber,
                PhoneNumberConfirmed = sourceUser.PhoneNumberConfirmed,
                Id = sourceUser.Id,
                Username = sourceUser.Username,
                Role = sourceUser.Role
            };

            // Lockout without an end date is stored as a far-future timestamp.
            if (Form.LockoutEnd.HasValue && Form.LockoutEnd.Value.Year >= 3000)
            {
                LockIndefinitely = true;
                Form.LockoutEnd = null;
            }

            if (Form is { LockoutEnabled: true } && sourceUser.Id == 0)
                ApplyNewUserLockDefaults();

            await InitDefaultsAsync();
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task InitDefaultsAsync()
    {
        if (Form is null)
            return;

        foreach (var departmentId in Form.DepartmentIds.Where(id => id > 0).Distinct())
            SelectedDepartmentIds.Add(departmentId);

        if (SelectedDepartmentIds.Count == 0 && Form.DepartmentId.HasValue)
            SelectedDepartmentIds.Add(Form.DepartmentId.Value);

        var resolvedRole = await ResolveExistingRoleAsync();
        Form.Role = !string.IsNullOrWhiteSpace(resolvedRole)
            ? resolvedRole
            : Form.Role ?? PMRolesConst.Tenant.Manger;
    }

    private async Task<string?> ResolveExistingRoleAsync()
    {
        if (UserForm.Id <= 0)
            return null;

        ApplicationUser? authUser = null;

        if (!string.IsNullOrWhiteSpace(UserForm.IdAuth))
            authUser = await UserManager.FindByIdAsync(UserForm.IdAuth);

        if (authUser is null && !string.IsNullOrWhiteSpace(UserForm.Username))
            authUser = await UserManager.FindByNameAsync(UserForm.Username);

        if (authUser is null && !string.IsNullOrWhiteSpace(UserForm.Email))
            authUser = await UserManager.FindByEmailAsync(UserForm.Email);

        if (authUser is null)
            return null;

        var roles = await UserManager.GetRolesAsync(authUser);
        return roles.FirstOrDefault();
    }

    protected void OnRoleChanged(string role)
    {
        if (Form is null)
            return;

        Form.Role = role;
    }

    protected void ToggleDepartment(int departmentId, bool selected)
    {
        if (selected)
        {
            if (!SelectedDepartmentIds.Contains(departmentId))
                SelectedDepartmentIds.Add(departmentId);
        }
        else
        {
            SelectedDepartmentIds.Remove(departmentId);
        }
    }

    protected void ToggleIndefinite(bool value)
    {
        LockIndefinitely = value;

        if (Form is null)
            return;

        if (value)
            Form.LockoutEnd = null;
    }

    protected void ToggleLockout(bool value)
    {
        if (Form is null)
            return;

        Form.LockoutEnabled = value;

        if (value)
        {
            ApplyNewUserLockDefaults();
            return;
        }

        LockIndefinitely = false;
        Form.LockoutStart = null;
        Form.LockoutEnd = null;
    }

    private void ApplyNewUserLockDefaults()
    {
        if (Form is null)
            return;

        LockIndefinitely = true;
        Form.LockoutStart ??= DateTimeOffset.Now.Date;
        Form.LockoutEnd = null;
    }

    protected void ClearLockoutStart()
    {
        if (Form is null) return;
        Form.LockoutStart = null;
    }

    protected void ClearLockoutEnd()
    {
        if (Form is null) return;
        Form.LockoutEnd = null;
    }

    protected async Task Cancel()
    {
        if (Callback.HasDelegate)
            await Callback.InvokeAsync(false);
    }

    private static bool IsValidEmail(string email)
        => System.Net.Mail.MailAddress.TryCreate(email, out var parsed)
           && parsed.Host.Contains('.');

    /// <summary>Field-level checks with explicit Swedish messages; the DTO carries no annotations.</summary>
    private bool Validate()
    {
        ValidationErrors.Clear();
        _fieldErrors.Clear();

        if (Form is null)
            return false;

        void AddError(string field, string message)
        {
            ValidationErrors.Add(message);
            _fieldErrors[field] = message;
        }

        if (string.IsNullOrWhiteSpace(Form.Firstname))
            AddError("firstname", "Förnamn är obligatoriskt.");

        if (string.IsNullOrWhiteSpace(Form.Lastname))
            AddError("lastname", "Efternamn är obligatoriskt.");

        if (string.IsNullOrWhiteSpace(Form.Email))
            AddError("email", "Email är obligatoriskt.");
        else if (!IsValidEmail(Form.Email.Trim()))
            AddError("email", "Email har ogiltigt format.");

        if (string.IsNullOrWhiteSpace(Form.Role))
            AddError("role", "Roll är obligatorisk.");
        else if (Form.Role != PMRolesConst.Tenant.Admin && SelectedDepartmentIds.Count == 0)
            AddError("department", "Minst en avdelning måste väljas.");

        return ValidationErrors.Count == 0;
    }

    protected async Task HandleSubmitAsync()
    {
        if (Form is null)
        {
            if (Callback.HasDelegate)
                await Callback.InvokeAsync(false);
            return;
        }

        if (IsSaving)
            return;

        if (!Validate())
        {
            await InvokeAsync(StateHasChanged);
            return;
        }

        Form.DepartmentIds = SelectedDepartmentIds.ToList();
        Form.DepartmentId = Form.Role == PMRolesConst.Tenant.Admin ? null : PrimaryDepartmentId;

        // "Tills vidare" — lockout with no end date is stored as a far-future timestamp.
        if (LockIndefinitely)
            Form.LockoutEnd = DateTimeOffset.MaxValue;

        IsSaving = true;

        try
        {
            Form.Email = Form.Email?.Trim();
            Form.Firstname = Form.Firstname?.Trim();
            Form.Lastname = Form.Lastname?.Trim();
            Form.PhoneNumber = Form.PhoneNumber?.Trim();
            Form.Username = string.IsNullOrWhiteSpace(Form.Username) ? Form.Email : Form.Username?.Trim();

            bool ok = UserForm.Id == 0
                ? await TenantUserService.RegisterAsync(Form)
                : await TenantUserService.UpdateUserAsync(Form);

            if (ok && UserForm.Id > 0)
                MHD.ToastInfo("Användaren har uppdaterats.");
            else
                MHD.Notifications(UserForm.Id == 0 ? ToastType.Add : ToastType.Update, ok);

            if (Callback.HasDelegate)
                await Callback.InvokeAsync(ok);
        }
        finally
        {
            IsSaving = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}
