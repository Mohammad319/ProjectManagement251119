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
    public void Calculation_is_editable_for_admin()
    {
        var calc = BuildCalculation(calcDept: OtherDept, createdBy: OtherUserId, shares: System.Array.Empty<ProjectShareEntity>());
        Assert.True(CanEditCalculation(calc, UserId, departmentId: null));
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static bool CanEditProject(ProjectEntity project, int userId, int? departmentId)
        => ProjectAccessRules.CanEdit(userId, departmentId).Compile()(project);

    private static bool CanEditCalculation(CalculationEntity calc, int userId, int? departmentId)
        => CalculationAccessRules.CanEdit(userId, departmentId).Compile()(calc);

    private static ProjectShareEntity ShareWithCalcs(string role, int userId, params int[] calcIds)
    {
        var share = ProjectShareEntity.ForUser(System.Guid.Empty, userId, role);
        share.ReplaceCalculations(calcIds);
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

    private static CalculationEntity BuildCalculation(int calcDept, int createdBy, ProjectShareEntity[] shares, int calcId = 1)
    {
        var project = BuildProject(folderDept: calcDept, createdBy: OtherUserId, shares);

        var calc = new CalculationEntity { Id = calcId, TenantId = 1 };
        _ = calc.Metadata;
        _ = calc.Sort;
        calc.AssignDepartment(calcDept);
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
