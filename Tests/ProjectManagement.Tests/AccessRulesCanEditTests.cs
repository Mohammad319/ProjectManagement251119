using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Project;
using Persistence.Service.Access;
using ProjectManagement.Shared.Constant;
using Xunit;

namespace ProjectManagement.Tests;

/// <summary>
/// Unit tests for the shared effective-edit rules used by both the project and calculation
/// save paths (and the resource/task grid writes). The rule is: a user may edit when they are
/// admin (tenant-wide), own the department, created the item, OR the project is shared with them
/// (direct or via department) with role "Användare" — and, for calculations, the calc is one of
/// the selected shared calculations. A "Visare" share never grants edit.
/// </summary>
public sealed class AccessRulesCanEditTests
{
    private const int OwnDept = 10;
    private const int OtherDept = 20;
    private const int UserId = 5;
    private const int OtherUserId = 999;

    // ── ProjectAccessRules.CanEdit ─────────────────────────────────────

    [Fact]
    public void Project_in_own_department_is_editable()
    {
        var project = BuildProject(folderDept: OwnDept, createdBy: OtherUserId);
        Assert.True(CanEditProject(project, UserId, OwnDept));
    }

    [Fact]
    public void Project_in_other_department_without_share_is_not_editable()
    {
        var project = BuildProject(folderDept: OtherDept, createdBy: OtherUserId);
        Assert.False(CanEditProject(project, UserId, OwnDept));
    }

    [Fact]
    public void Project_shared_with_user_as_Anvandare_is_editable()
    {
        var project = BuildProject(folderDept: OtherDept, createdBy: OtherUserId,
            ProjectShareEntity.ForUser(System.Guid.Empty, UserId, PMRolesConst.Tenant.Manger));
        Assert.True(CanEditProject(project, UserId, OwnDept));
    }

    [Fact]
    public void Project_shared_with_user_as_Visare_is_not_editable()
    {
        var project = BuildProject(folderDept: OtherDept, createdBy: OtherUserId,
            ProjectShareEntity.ForUser(System.Guid.Empty, UserId, PMRolesConst.Tenant.Viewer));
        Assert.False(CanEditProject(project, UserId, OwnDept));
    }

    [Fact]
    public void Project_shared_with_department_as_Anvandare_is_editable()
    {
        var project = BuildProject(folderDept: OtherDept, createdBy: OtherUserId,
            ProjectShareEntity.ForDepartment(System.Guid.Empty, OwnDept, PMRolesConst.Tenant.Manger));
        Assert.True(CanEditProject(project, UserId, OwnDept));
    }

    [Fact]
    public void Project_with_expired_share_is_not_visible_or_editable()
    {
        var share = ProjectShareEntity.ForUser(System.Guid.Empty, UserId, PMRolesConst.Tenant.Manger);
        share.SetValidUntil(System.DateTime.UtcNow.Date.AddDays(-1));
        var project = BuildProject(folderDept: OtherDept, createdBy: OtherUserId, share);

        Assert.False(CanSeeProject(project, UserId, OwnDept, isViewerOnly: true));
        Assert.False(CanEditProject(project, UserId, OwnDept));
    }

    [Fact]
    public void Project_share_valid_through_today_is_visible_and_editable()
    {
        var share = ProjectShareEntity.ForUser(System.Guid.Empty, UserId, PMRolesConst.Tenant.Manger);
        share.SetValidUntil(System.DateTime.UtcNow.Date);
        var project = BuildProject(folderDept: OtherDept, createdBy: OtherUserId, share);

        Assert.True(CanSeeProject(project, UserId, OwnDept, isViewerOnly: true));
        Assert.True(CanEditProject(project, UserId, OwnDept));
    }

    [Fact]
    public void Project_created_by_user_in_other_department_is_editable()
    {
        var project = BuildProject(folderDept: OtherDept, createdBy: UserId);
        Assert.True(CanEditProject(project, UserId, OwnDept));
    }

    [Fact]
    public void Project_is_editable_for_admin_even_without_share()
    {
        var project = BuildProject(folderDept: OtherDept, createdBy: OtherUserId);
        Assert.True(CanEditProject(project, UserId, departmentId: null));
    }

    // ── CalculationAccessRules.CanEdit ─────────────────────────────────

    [Fact]
    public void Calculation_in_own_department_is_editable()
    {
        var calc = BuildCalculation(calcDept: OwnDept, createdBy: OtherUserId, shares: System.Array.Empty<ProjectShareEntity>());
        Assert.True(CanEditCalculation(calc, UserId, OwnDept));
    }

    [Fact]
    public void Calculation_in_shared_project_as_Anvandare_with_calc_selected_is_editable()
    {
        var calc = BuildCalculation(calcDept: OtherDept, createdBy: OtherUserId, calcId: 1,
            shares: new[] { ShareWithCalcs(PMRolesConst.Tenant.Manger, UserId, 1) });
        Assert.True(CanEditCalculation(calc, UserId, OwnDept));
    }

    [Fact]
    public void Calculation_in_shared_project_as_Anvandare_but_calc_not_selected_is_not_editable()
    {
        var calc = BuildCalculation(calcDept: OtherDept, createdBy: OtherUserId, calcId: 1,
            shares: new[] { ShareWithCalcs(PMRolesConst.Tenant.Manger, UserId, 2) });
        Assert.False(CanEditCalculation(calc, UserId, OwnDept));
    }

    [Fact]
    public void Calculation_in_shared_project_as_Visare_is_not_editable()
    {
        var calc = BuildCalculation(calcDept: OtherDept, createdBy: OtherUserId, calcId: 1,
            shares: new[] { ShareWithCalcs(PMRolesConst.Tenant.Viewer, UserId, 1) });
        Assert.False(CanEditCalculation(calc, UserId, OwnDept));
    }

    [Fact]
    public void Calculation_in_expired_share_is_not_visible_or_editable()
    {
        var share = ShareWithCalcs(PMRolesConst.Tenant.Manger, UserId, 1);
        share.SetValidUntil(System.DateTime.UtcNow.Date.AddDays(-1));
        var calc = BuildCalculation(calcDept: OtherDept, createdBy: OtherUserId, calcId: 1, shares: new[] { share });

        Assert.False(CanSeeCalculation(calc, UserId, OwnDept, isViewerOnly: true));
        Assert.False(CanEditCalculation(calc, UserId, OwnDept));
    }

    [Fact]
    public void Calculation_is_editable_for_admin()
    {
        var calc = BuildCalculation(calcDept: OtherDept, createdBy: OtherUserId, shares: System.Array.Empty<ProjectShareEntity>());
        Assert.True(CanEditCalculation(calc, UserId, departmentId: null));
    }

    // ── Delningsomfattning: "Alla kalkyler i projektet" (AllCalculations) ──
    // En "alla kalkyler"-delning omfattar varje icke-privat kalkyl utan att kalkyl-id:t står i listan.

    [Fact]
    public void Calculation_in_all_calculations_share_as_Anvandare_is_editable_even_if_not_selected()
    {
        var calc = BuildCalculation(calcDept: OtherDept, createdBy: OtherUserId, calcId: 1,
            shares: new[] { ShareAllCalcs(PMRolesConst.Tenant.Manger, UserId) });
        Assert.True(CanEditCalculation(calc, UserId, OwnDept));
    }

    [Fact]
    public void Calculation_in_all_calculations_share_is_visible_to_viewer_even_if_not_selected()
    {
        var calc = BuildCalculation(calcDept: OtherDept, createdBy: OtherUserId, calcId: 1,
            shares: new[] { ShareAllCalcs(PMRolesConst.Tenant.Viewer, UserId) });
        Assert.True(CanSeeCalculation(calc, UserId, OwnDept, isViewerOnly: true));
    }

    [Fact]
    public void Calculation_in_all_calculations_share_is_visible_via_general_access()
    {
        var calc = BuildCalculation(calcDept: OtherDept, createdBy: OtherUserId, calcId: 1,
            shares: new[] { ShareAllCalcs(PMRolesConst.Tenant.Manger, UserId) });
        Assert.True(CanSeeCalculation(calc, UserId, OwnDept, isViewerOnly: false));
    }

    // ── Privat kalkyl undantas ALLTID från delning (även "alla kalkyler") ──

    [Fact]
    public void Private_calculation_in_all_calculations_share_is_not_editable()
    {
        var calc = BuildCalculation(calcDept: OtherDept, createdBy: OtherUserId, calcId: 1,
            shares: new[] { ShareAllCalcs(PMRolesConst.Tenant.Manger, UserId) }, isPrivate: true);
        Assert.False(CanEditCalculation(calc, UserId, OwnDept));
    }

    [Fact]
    public void Private_calculation_in_all_calculations_share_is_not_visible_to_viewer()
    {
        var calc = BuildCalculation(calcDept: OtherDept, createdBy: OtherUserId, calcId: 1,
            shares: new[] { ShareAllCalcs(PMRolesConst.Tenant.Viewer, UserId) }, isPrivate: true);
        Assert.False(CanSeeCalculation(calc, UserId, OwnDept, isViewerOnly: true));
    }

    [Fact]
    public void Private_calculation_in_all_calculations_share_is_not_visible_via_general_access()
    {
        var calc = BuildCalculation(calcDept: OtherDept, createdBy: OtherUserId, calcId: 1,
            shares: new[] { ShareAllCalcs(PMRolesConst.Tenant.Manger, UserId) }, isPrivate: true);
        Assert.False(CanSeeCalculation(calc, UserId, OwnDept, isViewerOnly: false));
    }

    [Fact]
    public void Private_calculation_explicitly_selected_in_share_is_still_not_visible_to_viewer()
    {
        // Även om id:t skulle ligga i den valda listan ska en privat kalkyl aldrig läcka till en delad användare.
        var calc = BuildCalculation(calcDept: OtherDept, createdBy: OtherUserId, calcId: 1,
            shares: new[] { ShareWithCalcs(PMRolesConst.Tenant.Viewer, UserId, 1) }, isPrivate: true);
        Assert.False(CanSeeCalculation(calc, UserId, OwnDept, isViewerOnly: true));
    }

    [Fact]
    public void Selected_calculations_share_still_excludes_unselected_calc_for_viewer()
    {
        // "Valda kalkyler"-läget (utan AllCalculations) ska fortsatt bara omfatta de valda id:na.
        var calc = BuildCalculation(calcDept: OtherDept, createdBy: OtherUserId, calcId: 1,
            shares: new[] { ShareWithCalcs(PMRolesConst.Tenant.Viewer, UserId, 2) });
        Assert.False(CanSeeCalculation(calc, UserId, OwnDept, isViewerOnly: true));
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static bool CanEditProject(ProjectEntity project, int userId, int? departmentId)
        => ProjectAccessRules.CanEdit(userId, departmentId).Compile()(project);

    private static bool CanSeeProject(ProjectEntity project, int userId, int? departmentId, bool isViewerOnly)
        => ProjectAccessRules.CanSee(userId, departmentId, isViewerOnly).Compile()(project);

    private static bool CanEditCalculation(CalculationEntity calc, int userId, int? departmentId)
        => CalculationAccessRules.CanEdit(userId, departmentId).Compile()(calc);

    private static bool CanSeeCalculation(CalculationEntity calc, int userId, int? departmentId, bool isViewerOnly)
        => CalculationAccessRules.CanSee(userId, departmentId, isViewerOnly).Compile()(calc);

    private static ProjectShareEntity ShareWithCalcs(string role, int userId, params int[] calcIds)
    {
        var share = ProjectShareEntity.ForUser(System.Guid.Empty, userId, role);
        share.ReplaceCalculations(calcIds);
        return share;
    }

    // "Alla kalkyler i projektet"-delning: ingen explicit kalkyl-lista, omfattar alla icke-privata.
    private static ProjectShareEntity ShareAllCalcs(string role, int userId)
    {
        var share = ProjectShareEntity.ForUser(System.Guid.Empty, userId, role);
        share.SetAllCalculations(true);
        return share;
    }

    private static ProjectEntity BuildProject(int folderDept, int createdBy, params ProjectShareEntity[] shares)
    {
        var folder = new FolderEntity("Folder", "#08BF66", folderDept, createdBy: 1, sortOrder: 0);

        var project = (ProjectEntity)System.Activator.CreateInstance(typeof(ProjectEntity), nonPublic: true)!;
        project.Id = System.Guid.NewGuid();
        project.TenantId = 1;
        _ = project.Metadata;
        SetPrivate(project, nameof(ProjectEntity.Folder), folder);
        SetPrivate(project, nameof(ProjectEntity.FolderId), folder.Id);
        SetPrivate(project, "CreatedBy", createdBy);
        SetPrivate(project, nameof(ProjectEntity.Shares), new List<ProjectShareEntity>(shares));
        return project;
    }

    private static CalculationEntity BuildCalculation(int calcDept, int createdBy, ProjectShareEntity[] shares, int calcId = 1, bool isPrivate = false)
    {
        var project = BuildProject(folderDept: calcDept, createdBy: OtherUserId, shares);

        var calc = new CalculationEntity { Id = calcId, TenantId = 1 };
        _ = calc.Metadata;
        _ = calc.Sort;
        calc.AssignDepartment(calcDept);
        calc.SetVisibility(isPrivate, isArchived: false);
        SetPrivate(calc, "CreatedBy", createdBy);
        SetPrivate(calc, nameof(CalculationEntity.Project), project);
        SetPrivate(calc, nameof(CalculationEntity.ProjectId), project.Id);
        SetPrivate(calc, nameof(CalculationEntity.Name), $"Calc {calcId}");
        SetPrivate(calc, nameof(CalculationEntity.Code), $"C-{calcId}");
        project.Calculations.Add(calc);
        return calc;
    }

    private static void SetPrivate<T>(object target, string propertyName, T value)
    {
        var property = target.GetType().GetProperty(propertyName)
            ?? throw new System.InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().Name}.");
        property.SetValue(target, value);
    }
}
