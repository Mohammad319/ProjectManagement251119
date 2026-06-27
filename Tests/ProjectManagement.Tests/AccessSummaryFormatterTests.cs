using System.Collections.Generic;
using System.Linq;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Helper;
using Xunit;

namespace ProjectManagement.Tests;

/// <summary>
/// Unit tests for <see cref="AccessSummaryFormatter"/> — the pure formatting/classification used by
/// the "Åtkomst" column in the project list (ProjectsUI) and calculation list (ProjectsCalculationList).
/// Covers the compact summary text and the grouped filter tag-set for both lists.
/// </summary>
public sealed class AccessSummaryFormatterTests
{
    private const string Viewer = PMRolesConst.Tenant.Viewer;
    private const string Manager = PMRolesConst.Tenant.Manger;

    private static ProjectAccessRecipientDTO User(string role, string name, int calcCount = 0) =>
        new() { Type = ProjectShareRecipientType.User, Name = name, Role = role, CalcCount = calcCount };

    private static ProjectAccessRecipientDTO Dept(string role, string name, int calcCount = 0) =>
        new() { Type = ProjectShareRecipientType.Department, Name = name, Role = role, CalcCount = calcCount };

    private static ProjectAccessSummaryDTO ProjAccess(bool viaDept, int shareable, params ProjectAccessRecipientDTO[] recipients) =>
        new() { ViaDepartment = viaDept, ShareableCalcCount = shareable, Recipients = recipients.ToList() };

    private static CalculationAccessSummaryDTO CalcAccess(bool viaProject, bool isPrivate, params ProjectAccessRecipientDTO[] recipients) =>
        new() { ViaProject = viaProject, IsPrivate = isPrivate, Recipients = recipients.ToList() };

    [Fact]
    public void Access_type_filter_uses_clear_swedish_labels()
    {
        Assert.Equal("Avdelningsåtkomst", AccessSummaryFormatter.ProjectTypeDepartmentOnly);
        Assert.Equal("Delat", AccessSummaryFormatter.TypeShared);
        Assert.Equal("Delvis delat", AccessSummaryFormatter.ProjectTypeLimited);
    }

    // ── RoleLabel / AccessLevelLabel ─────────────────────────────────────────

    [Fact]
    public void RoleLabel_maps_known_roles_and_falls_back()
    {
        Assert.Equal("Visare", AccessSummaryFormatter.RoleLabel(Viewer));
        Assert.Equal("Användare", AccessSummaryFormatter.RoleLabel(Manager));
        Assert.Equal("Admin", AccessSummaryFormatter.RoleLabel(PMRolesConst.Tenant.Admin));
        Assert.Equal("—", AccessSummaryFormatter.RoleLabel(""));
        Assert.Equal("—", AccessSummaryFormatter.RoleLabel(null));
    }

    [Fact]
    public void AccessLevelLabel_uses_share_language()
    {
        Assert.Equal("Kan ändra", AccessSummaryFormatter.AccessLevelLabel(Manager));
        Assert.Equal("Kan ändra", AccessSummaryFormatter.AccessLevelLabel(PMRolesConst.Tenant.Admin));
        Assert.Equal("Kan visa", AccessSummaryFormatter.AccessLevelLabel(Viewer));
        Assert.Equal("Kan visa", AccessSummaryFormatter.AccessLevelLabel(null));
    }

    [Fact]
    public void RecipientFilterValue_uses_person_or_department_prefix()
    {
        Assert.Equal("Avdelning: Produktion", AccessSummaryFormatter.RecipientFilterValue(Dept(Viewer, "Produktion")));
        Assert.Equal("Person: Nordbygg Visare 01", AccessSummaryFormatter.RecipientFilterValue(User(Viewer, "Nordbygg Visare 01")));
        Assert.Equal("Person: Nordbygg Användare 01", AccessSummaryFormatter.RecipientFilterValue(User(Manager, "Nordbygg Användare 01")));
    }

    // ── Project list: summary text ───────────────────────────────────────────

    [Fact]
    public void Project_null_access_uses_fallback_flag()
    {
        Assert.Equal("Avdelningsåtkomst", AccessSummaryFormatter.ProjectSummaryText(null, fallbackIsShared: false));
        Assert.Equal("Delat", AccessSummaryFormatter.ProjectSummaryText(null, fallbackIsShared: true));
    }

    [Fact]
    public void Project_no_recipients_via_department_shows_avdelningsatkomst()
    {
        var access = ProjAccess(viaDept: true, shareable: 5);
        Assert.Equal("Avdelningsåtkomst", AccessSummaryFormatter.ProjectSummaryText(access, false));
        Assert.False(AccessSummaryFormatter.ProjectIsShared(access, false));
    }

    [Fact]
    public void Project_single_person_full_calcs_shows_short_shared_status()
    {
        var access = ProjAccess(viaDept: false, shareable: 5, User(Viewer, "Nordbygg Visare 02", calcCount: 5));
        Assert.Equal("Delat", AccessSummaryFormatter.ProjectSummaryText(access, false));
    }

    [Fact]
    public void Project_single_department_full_calcs_shows_short_shared_status()
    {
        var access = ProjAccess(viaDept: false, shareable: 5, Dept(Viewer, "Produktion", calcCount: 5));
        Assert.Equal("Delat", AccessSummaryFormatter.ProjectSummaryText(access, false));
    }

    [Fact]
    public void Project_mixed_recipients_full_calcs_shows_short_shared_status()
    {
        var access = ProjAccess(viaDept: false, shareable: 5,
            User(Manager, "Anna", 5), User(Viewer, "Bo", 5), Dept(Viewer, "Ledning", 5));
        Assert.Equal("Delat", AccessSummaryFormatter.ProjectSummaryText(access, false));
    }

    [Fact]
    public void Project_two_departments_full_calcs_shows_short_shared_status()
    {
        var access = ProjAccess(viaDept: false, shareable: 5,
            Dept(Viewer, "Produktion", 5), Dept(Manager, "Ledning", 5));
        Assert.Equal("Delat", AccessSummaryFormatter.ProjectSummaryText(access, false));
    }

    [Fact]
    public void Project_limited_calcs_shows_short_limited_status()
    {
        var access = ProjAccess(viaDept: false, shareable: 5, User(Viewer, "Visare 02", calcCount: 3));
        Assert.Equal("Delvis delat", AccessSummaryFormatter.ProjectSummaryText(access, false));
    }

    [Fact]
    public void Project_limited_uses_least_covered_of_total()
    {
        var access = ProjAccess(viaDept: false, shareable: 5,
            User(Viewer, "A", 2), User(Viewer, "B", 3), User(Viewer, "C", 1));
        Assert.Equal("Delvis delat", AccessSummaryFormatter.ProjectSummaryText(access, false));
    }

    [Fact]
    public void Project_limited_mixed_full_and_partial_never_shows_complete_fraction()
    {
        // One recipient has all calcs, another is limited — must NOT read as "2/2".
        var access = ProjAccess(viaDept: false, shareable: 2,
            User(Manager, "Full", 2), User(Viewer, "Partial", 1));
        Assert.Equal("Delvis delat", AccessSummaryFormatter.ProjectSummaryText(access, false));
    }

    // ── Project list: filter tags ────────────────────────────────────────────

    [Fact]
    public void Project_tags_for_not_shared_is_department_only()
    {
        var access = ProjAccess(viaDept: true, shareable: 3);
        var tags = AccessSummaryFormatter.ProjectTags(access, false).ToList();
        Assert.Contains(AccessSummaryFormatter.ProjectTypeDepartmentOnly, tags);
        Assert.DoesNotContain(AccessSummaryFormatter.TypeShared, tags);
    }

    [Fact]
    public void Project_tags_for_shared_include_level_recipient_and_limited()
    {
        var access = ProjAccess(viaDept: true, shareable: 5,
            User(Manager, "Anna", 5), Dept(Viewer, "Produktion", 2));
        var tags = AccessSummaryFormatter.ProjectTags(access, false).ToList();

        Assert.Contains(AccessSummaryFormatter.ProjectTypeLimited, tags);   // Produktion only has 2/5
        Assert.DoesNotContain(AccessSummaryFormatter.TypeShared, tags);
        Assert.DoesNotContain(AccessSummaryFormatter.LevelEdit, tags);
        Assert.DoesNotContain(AccessSummaryFormatter.LevelView, tags);
        Assert.DoesNotContain("Person: Anna", tags);
        Assert.DoesNotContain("Avdelning: Produktion", tags);
        Assert.DoesNotContain(AccessSummaryFormatter.ProjectTypeDepartmentOnly, tags);
    }

    [Fact]
    public void Project_tags_not_limited_when_all_recipients_cover_everything()
    {
        var access = ProjAccess(viaDept: false, shareable: 4, User(Viewer, "A", 4));
        var tags = AccessSummaryFormatter.ProjectTags(access, false).ToList();
        Assert.DoesNotContain(AccessSummaryFormatter.ProjectTypeLimited, tags);
    }

    [Fact]
    public void Project_tags_null_access_fallback_shared()
    {
        var tags = AccessSummaryFormatter.ProjectTags(null, fallbackIsShared: true).ToList();
        Assert.Contains(AccessSummaryFormatter.TypeShared, tags);
        Assert.DoesNotContain(AccessSummaryFormatter.ProjectTypeDepartmentOnly, tags);
    }

    // ── Calculation list: summary text ───────────────────────────────────────

    [Fact]
    public void Calc_private_shows_privat()
    {
        var access = CalcAccess(viaProject: true, isPrivate: true);
        Assert.Equal("Privat", AccessSummaryFormatter.CalcSummaryText(access, isPrivate: true));
        Assert.Equal("Privat", AccessSummaryFormatter.CalcSummaryText(null, isPrivate: true));
    }

    [Fact]
    public void Calc_null_access_is_via_project()
    {
        Assert.Equal("Avdelningsåtkomst", AccessSummaryFormatter.CalcSummaryText(null, isPrivate: false));
        Assert.Equal("Ej via projekt", AccessSummaryFormatter.CalcSummaryText(null, isPrivate: false, projectHasSharing: true));
    }

    [Fact]
    public void Calc_no_recipients_via_project()
    {
        var access = CalcAccess(viaProject: true, isPrivate: false);
        Assert.Equal("Avdelningsåtkomst", AccessSummaryFormatter.CalcSummaryText(access, false));
        Assert.Equal("Ej via projekt", AccessSummaryFormatter.CalcSummaryText(access, false, projectHasSharing: true));
        Assert.False(AccessSummaryFormatter.CalcIsShared(access));
    }

    [Fact]
    public void Calc_shared_shows_compact_recipient_count()
    {
        var access = CalcAccess(viaProject: false, isPrivate: false,
            User(Viewer, "A"), User(Viewer, "B"));
        Assert.Equal("Via projekt", AccessSummaryFormatter.CalcSummaryText(access, false));

        var mixed = CalcAccess(viaProject: false, isPrivate: false,
            User(Manager, "A"), Dept(Viewer, "Produktion"));
        Assert.Equal("Via projekt", AccessSummaryFormatter.CalcSummaryText(mixed, false));
    }

    // ── Calculation list: filter tags ────────────────────────────────────────

    [Fact]
    public void Calc_private_tags_only_private()
    {
        var tags = AccessSummaryFormatter.CalcTags(CalcAccess(true, true), isPrivate: true).ToList();
        Assert.Equal(new[] { AccessSummaryFormatter.CalcTypePrivate }, tags);
    }

    [Fact]
    public void Calc_shared_tags_include_limited_level_and_recipients()
    {
        var access = CalcAccess(viaProject: true, isPrivate: false,
            User(Manager, "Anna"), Dept(Viewer, "Produktion"));
        var tags = AccessSummaryFormatter.CalcTags(access, false).ToList();

        Assert.Equal(new[] { AccessSummaryFormatter.CalcTypeViaProject }, tags);
        Assert.DoesNotContain(AccessSummaryFormatter.CalcTypeProjectOnly, tags);
    }

    [Fact]
    public void Calc_not_shared_tags_project_only()
    {
        var tags = AccessSummaryFormatter.CalcTags(CalcAccess(viaProject: true, isPrivate: false), false).ToList();
        Assert.Contains(AccessSummaryFormatter.CalcTypeDepartmentAccess, tags);
        Assert.DoesNotContain(AccessSummaryFormatter.TypeShared, tags);
        Assert.DoesNotContain(AccessSummaryFormatter.TypeLimited, tags);
    }

    [Fact]
    public void Calc_not_in_project_share_tags_not_via_project()
    {
        var tags = AccessSummaryFormatter.CalcTags(
            CalcAccess(viaProject: true, isPrivate: false),
            false,
            projectHasSharing: true).ToList();

        Assert.Contains(AccessSummaryFormatter.CalcTypeNotViaProject, tags);
        Assert.DoesNotContain(AccessSummaryFormatter.TypeShared, tags);
        Assert.DoesNotContain(AccessSummaryFormatter.TypeLimited, tags);
    }
}
