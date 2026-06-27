using System.Collections.Generic;
using System.Linq;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Helper;
using Xunit;

namespace ProjectManagement.Tests;

/// <summary>
/// Unit tests for <see cref="ProjectAccessFilter"/> — the detailed "Åtkomst" filter in the project
/// list (ProjectsUI). The column stays simple (Avdelningsåtkomst / Delat / Delvis delat) but the
/// filter is faceted across four groups (Åtkomsttyp, Åtkomstnivå, Personer, Avdelningar) and matches
/// OR within a group, AND across groups.
/// </summary>
public sealed class ProjectAccessFilterTests
{
    private const string Edit = PMRolesConst.Tenant.Manger;   // "Användare" → Kan ändra
    private const string View = PMRolesConst.Tenant.Viewer;   // "Visare"    → Kan visa

    private static ProjectAccessRecipientDTO User(string role, string name, int calcCount = 0) =>
        new() { Type = ProjectShareRecipientType.User, Name = name, Role = role, CalcCount = calcCount };

    private static ProjectAccessRecipientDTO Dept(string role, string name, int calcCount = 0) =>
        new() { Type = ProjectShareRecipientType.Department, Name = name, Role = role, CalcCount = calcCount };

    private static ListProjectMVVM Project(bool viaDept, int shareable, params ProjectAccessRecipientDTO[] recipients) =>
        new()
        {
            IsShared = recipients.Length > 0,
            Access = new ProjectAccessSummaryDTO
            {
                ViaDepartment = viaDept,
                ShareableCalcCount = shareable,
                Recipients = recipients.ToList()
            }
        };

    // Department-only access (no extra sharing) → column/filter type "Avdelningsåtkomst".
    private static readonly ListProjectMVVM DeptOnly = Project(viaDept: true, shareable: 3);
    // Shared as "Kan ändra" with the person Anna, all calcs → "Delat".
    private static readonly ListProjectMVVM SharedEdit = Project(viaDept: false, shareable: 5, User(Edit, "Anna", 5));
    // Shared as "Kan visa" with the department Produktion, all calcs → "Delat".
    private static readonly ListProjectMVVM SharedView = Project(viaDept: false, shareable: 5, Dept(View, "Produktion", 5));
    // Shared with Bo but only some calcs → "Delvis delat".
    private static readonly ListProjectMVVM Partial = Project(viaDept: false, shareable: 5, User(View, "Bo", 2));

    private static readonly List<ListProjectMVVM> All = [DeptOnly, SharedEdit, SharedView, Partial];

    private static List<ListProjectMVVM> Filter(IEnumerable<string> selected) =>
        ProjectAccessFilter.Apply(All, new HashSet<string>(selected)).ToList();

    // ── BuildGroups ──────────────────────────────────────────────────────────

    [Fact]
    public void BuildGroups_has_type_and_level_plus_person_and_department_when_present()
    {
        var groups = ProjectAccessFilter.BuildGroups(All);
        var headers = groups.Select(g => g.Header).ToList();

        Assert.Equal(new[]
        {
            AccessSummaryFormatter.GroupAccessType,
            AccessSummaryFormatter.GroupAccessLevel,
            AccessSummaryFormatter.GroupPersons,
            AccessSummaryFormatter.GroupDepartments,
        }, headers);

        var persons = groups.Single(g => g.Header == AccessSummaryFormatter.GroupPersons).Options.Select(o => o.Value).ToList();
        Assert.Contains("Anna", persons);
        Assert.Contains("Bo", persons);

        var depts = groups.Single(g => g.Header == AccessSummaryFormatter.GroupDepartments).Options.Select(o => o.Value).ToList();
        Assert.Equal(new[] { "Produktion" }, depts);
    }

    [Fact]
    public void BuildGroups_omits_person_and_department_groups_when_nothing_is_shared()
    {
        var groups = ProjectAccessFilter.BuildGroups(new[] { DeptOnly });
        var headers = groups.Select(g => g.Header).ToList();

        Assert.Equal(new[] { AccessSummaryFormatter.GroupAccessType, AccessSummaryFormatter.GroupAccessLevel }, headers);
    }

    [Fact]
    public void Filter_headers_use_normal_swedish_casing_not_uppercase()
    {
        // Acceptance: "Åtkomsttyp" not "ÅTKOMSTTYP".
        Assert.Equal("Åtkomsttyp", AccessSummaryFormatter.GroupAccessType);
        Assert.Equal("Åtkomstnivå", AccessSummaryFormatter.GroupAccessLevel);
        Assert.Equal("Personer", AccessSummaryFormatter.GroupPersons);
        Assert.Equal("Avdelningar", AccessSummaryFormatter.GroupDepartments);

        foreach (var header in ProjectAccessFilter.BuildGroups(All).Select(g => g.Header))
            Assert.NotEqual(header.ToUpperInvariant(), header);
    }

    // ── ValuesForGroup ───────────────────────────────────────────────────────

    [Fact]
    public void ValuesForGroup_maps_each_dimension()
    {
        Assert.Contains(AccessSummaryFormatter.TypeShared,
            ProjectAccessFilter.ValuesForGroup(SharedEdit, AccessSummaryFormatter.GroupAccessType));
        Assert.Contains(AccessSummaryFormatter.LevelEdit,
            ProjectAccessFilter.ValuesForGroup(SharedEdit, AccessSummaryFormatter.GroupAccessLevel));
        Assert.Contains(AccessSummaryFormatter.LevelView,
            ProjectAccessFilter.ValuesForGroup(SharedView, AccessSummaryFormatter.GroupAccessLevel));
        Assert.Contains("Anna",
            ProjectAccessFilter.ValuesForGroup(SharedEdit, AccessSummaryFormatter.GroupPersons));
        Assert.Contains("Produktion",
            ProjectAccessFilter.ValuesForGroup(SharedView, AccessSummaryFormatter.GroupDepartments));
    }

    [Fact]
    public void ValuesForGroup_for_department_only_project_has_no_level_or_recipients()
    {
        Assert.Empty(ProjectAccessFilter.ValuesForGroup(DeptOnly, AccessSummaryFormatter.GroupAccessLevel));
        Assert.Empty(ProjectAccessFilter.ValuesForGroup(DeptOnly, AccessSummaryFormatter.GroupPersons));
        Assert.Contains(AccessSummaryFormatter.ProjectTypeDepartmentOnly,
            ProjectAccessFilter.ValuesForGroup(DeptOnly, AccessSummaryFormatter.GroupAccessType));
    }

    // ── Matching ─────────────────────────────────────────────────────────────

    [Fact]
    public void Empty_selection_matches_everything()
    {
        Assert.Equal(All.Count, Filter([]).Count);
    }

    [Fact]
    public void Type_filter_selects_only_matching_type()
    {
        var shared = Filter([AccessSummaryFormatter.TypeShared]);
        Assert.Contains(SharedEdit, shared);
        Assert.Contains(SharedView, shared);
        Assert.DoesNotContain(DeptOnly, shared);
        Assert.DoesNotContain(Partial, shared);   // "Delvis delat" is a different type
    }

    [Fact]
    public void Type_filter_is_or_within_group()
    {
        var result = Filter([AccessSummaryFormatter.ProjectTypeDepartmentOnly, AccessSummaryFormatter.ProjectTypeLimited]);
        Assert.Contains(DeptOnly, result);
        Assert.Contains(Partial, result);
        Assert.DoesNotContain(SharedEdit, result);
        Assert.DoesNotContain(SharedView, result);
    }

    [Fact]
    public void Level_filter_selects_by_share_role()
    {
        var canChange = Filter([AccessSummaryFormatter.LevelEdit]);
        Assert.Equal(new[] { SharedEdit }, canChange);

        var canView = Filter([AccessSummaryFormatter.LevelView]);
        Assert.Contains(SharedView, canView);
        Assert.Contains(Partial, canView);
        Assert.DoesNotContain(SharedEdit, canView);
        Assert.DoesNotContain(DeptOnly, canView);
    }

    [Fact]
    public void Person_and_department_filters_select_by_recipient()
    {
        Assert.Equal(new[] { SharedEdit }, Filter(["Anna"]));
        Assert.Equal(new[] { SharedView }, Filter(["Produktion"]));
    }

    [Fact]
    public void Cross_group_selection_is_and()
    {
        // "Delat" AND "Kan ändra": SharedEdit qualifies, SharedView is "Delat" but only "Kan visa".
        var result = Filter([AccessSummaryFormatter.TypeShared, AccessSummaryFormatter.LevelEdit]);
        Assert.Equal(new[] { SharedEdit }, result);
    }

    [Fact]
    public void Cross_group_with_no_match_returns_empty()
    {
        // "Avdelningsåtkomst" (DeptOnly) AND "Kan ändra" (needs a share) → impossible.
        var result = Filter([AccessSummaryFormatter.ProjectTypeDepartmentOnly, AccessSummaryFormatter.LevelEdit]);
        Assert.Empty(result);
    }
}
