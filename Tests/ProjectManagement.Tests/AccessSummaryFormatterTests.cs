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
/// Covers the compact summary text and the filter tag-set for both lists.
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

    // ── RoleLabel ──────────────────────────────────────────────────────────

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
    public void RecipientFilterTag_uses_department_or_role_prefix()
    {
        Assert.Equal("Avdelning: Produktion", AccessSummaryFormatter.RecipientFilterTag(Dept(Viewer, "Produktion")));
        Assert.Equal("Visare: Nordbygg Visare 01", AccessSummaryFormatter.RecipientFilterTag(User(Viewer, "Nordbygg Visare 01")));
        Assert.Equal("Användare: Nordbygg Användare 01", AccessSummaryFormatter.RecipientFilterTag(User(Manager, "Nordbygg Användare 01")));
    }

    // ── Project list: summary text ───────────────────────────────────────────

    [Fact]
    public void Project_null_access_uses_fallback_flag()
    {
        Assert.Equal("Via avdelning", AccessSummaryFormatter.ProjectSummaryText(null, fallbackIsShared: false));
        Assert.Equal("Delad", AccessSummaryFormatter.ProjectSummaryText(null, fallbackIsShared: true));
    }

    [Fact]
    public void Project_no_recipients_via_department_shows_via_avdelning()
    {
        var access = ProjAccess(viaDept: true, shareable: 5);
        Assert.Equal("Via avdelning", AccessSummaryFormatter.ProjectSummaryText(access, false));
        Assert.False(AccessSummaryFormatter.ProjectIsShared(access, false));
    }

    [Fact]
    public void Project_single_recipient_shows_role_and_name()
    {
        var access = ProjAccess(viaDept: false, shareable: 5, User(Viewer, "Nordbygg Visare 02", calcCount: 5));
        Assert.Equal("Visare: Nordbygg Visare 02", AccessSummaryFormatter.ProjectSummaryText(access, false));
    }

    [Fact]
    public void Project_single_recipient_with_limited_calcs_shows_fraction()
    {
        var access = ProjAccess(viaDept: false, shareable: 5, User(Viewer, "Visare 02", calcCount: 3));
        Assert.Equal("Visare: Visare 02 · 3/5 kalkyler", AccessSummaryFormatter.ProjectSummaryText(access, false));
    }

    [Fact]
    public void Project_two_recipients_show_names()
    {
        var access = ProjAccess(viaDept: false, shareable: 5,
            User(Manager, "Anna", calcCount: 5),
            User(Viewer, "Bo", calcCount: 5));
        Assert.Equal("Anna · Bo", AccessSummaryFormatter.ProjectSummaryText(access, false));
    }

    [Fact]
    public void Project_many_recipients_summarise_by_permission()
    {
        var access = ProjAccess(viaDept: false, shareable: 5,
            User(Manager, "A", 5), User(Viewer, "B", 5), User(Viewer, "C", 5));
        Assert.Equal("Användare 1 · Visare 2", AccessSummaryFormatter.ProjectSummaryText(access, false));
    }

    [Fact]
    public void Project_many_recipients_append_calc_fraction_when_limited()
    {
        var access = ProjAccess(viaDept: false, shareable: 5,
            User(Viewer, "A", 2), User(Viewer, "B", 3), User(Viewer, "C", 1));
        // max covered calcs (3) of 5.
        Assert.Equal("Visare 3 · 3/5 kalkyler", AccessSummaryFormatter.ProjectSummaryText(access, false));
    }

    // ── Project list: filter tags ────────────────────────────────────────────

    [Fact]
    public void Project_tags_for_not_shared_via_department()
    {
        var access = ProjAccess(viaDept: true, shareable: 3);
        var tags = AccessSummaryFormatter.ProjectTags(access, false).ToList();
        Assert.Contains(AccessSummaryFormatter.ProjectTagNotShared, tags);
        Assert.Contains(AccessSummaryFormatter.ProjectTagViaDepartment, tags);
        Assert.DoesNotContain(AccessSummaryFormatter.ProjectTagShared, tags);
    }

    [Fact]
    public void Project_tags_for_shared_include_permission_recipient_and_limited()
    {
        var access = ProjAccess(viaDept: true, shareable: 5,
            User(Manager, "Anna", 5), Dept(Viewer, "Produktion", 2));
        var tags = AccessSummaryFormatter.ProjectTags(access, false).ToList();

        Assert.Contains(AccessSummaryFormatter.ProjectTagShared, tags);
        Assert.Contains(AccessSummaryFormatter.ProjectTagViaDepartment, tags);
        Assert.Contains(AccessSummaryFormatter.ProjectTagLimited, tags);          // Produktion only has 2/5
        Assert.Contains(AccessSummaryFormatter.PermManagerTag, tags);
        Assert.Contains(AccessSummaryFormatter.PermViewerTag, tags);
        Assert.Contains("Användare: Anna", tags);
        Assert.Contains("Avdelning: Produktion", tags);
        Assert.DoesNotContain(AccessSummaryFormatter.ProjectTagNotShared, tags);
    }

    [Fact]
    public void Project_tags_not_limited_when_all_recipients_cover_everything()
    {
        var access = ProjAccess(viaDept: false, shareable: 4, User(Viewer, "A", 4));
        var tags = AccessSummaryFormatter.ProjectTags(access, false).ToList();
        Assert.DoesNotContain(AccessSummaryFormatter.ProjectTagLimited, tags);
    }

    [Fact]
    public void Project_tags_null_access_fallback_shared()
    {
        var tags = AccessSummaryFormatter.ProjectTags(null, fallbackIsShared: true).ToList();
        Assert.Contains(AccessSummaryFormatter.ProjectTagShared, tags);
        Assert.DoesNotContain(AccessSummaryFormatter.ProjectTagViaDepartment, tags);
    }

    // ── Calculation list: summary text ───────────────────────────────────────

    [Fact]
    public void Calc_private_shows_owner_admin_only()
    {
        var access = CalcAccess(viaProject: true, isPrivate: true);
        Assert.Equal("Endast ägare/Admin", AccessSummaryFormatter.CalcSummaryText(access, isPrivate: true));
        // isPrivate flag wins even if access says otherwise.
        Assert.Equal("Endast ägare/Admin", AccessSummaryFormatter.CalcSummaryText(null, isPrivate: true));
    }

    [Fact]
    public void Calc_null_access_is_via_project()
    {
        Assert.Equal("Via projekt", AccessSummaryFormatter.CalcSummaryText(null, isPrivate: false));
    }

    [Fact]
    public void Calc_no_recipients_via_project()
    {
        var access = CalcAccess(viaProject: true, isPrivate: false);
        Assert.Equal("Via projekt", AccessSummaryFormatter.CalcSummaryText(access, false));
        Assert.False(AccessSummaryFormatter.CalcIsShared(access));
    }

    [Fact]
    public void Calc_shared_summarises_by_permission()
    {
        var access = CalcAccess(viaProject: false, isPrivate: false,
            User(Viewer, "A"), User(Viewer, "B"));
        Assert.Equal("Delad · Visare 2", AccessSummaryFormatter.CalcSummaryText(access, false));

        var mixed = CalcAccess(viaProject: false, isPrivate: false,
            User(Manager, "A"), User(Viewer, "B"), User(Viewer, "C"));
        Assert.Equal("Delad · Användare 1 · Visare 2", AccessSummaryFormatter.CalcSummaryText(mixed, false));
    }

    // ── Calculation list: filter tags ────────────────────────────────────────

    [Fact]
    public void Calc_private_tags_only_private()
    {
        var tags = AccessSummaryFormatter.CalcTags(CalcAccess(true, true), isPrivate: true).ToList();
        Assert.Equal(new[] { AccessSummaryFormatter.CalcTagPrivate }, tags);
    }

    [Fact]
    public void Calc_shared_tags_include_limited_permission_and_recipients()
    {
        var access = CalcAccess(viaProject: true, isPrivate: false,
            User(Manager, "Anna"), Dept(Viewer, "Produktion"));
        var tags = AccessSummaryFormatter.CalcTags(access, false).ToList();

        Assert.Contains(AccessSummaryFormatter.CalcTagShared, tags);
        Assert.Contains(AccessSummaryFormatter.CalcTagViaProject, tags);
        Assert.Contains(AccessSummaryFormatter.CalcTagLimited, tags);
        Assert.Contains(AccessSummaryFormatter.PermManagerTag, tags);
        Assert.Contains(AccessSummaryFormatter.PermViewerTag, tags);
        Assert.Contains("Användare: Anna", tags);
        Assert.Contains("Avdelning: Produktion", tags);
    }

    [Fact]
    public void Calc_not_shared_tags_via_project()
    {
        var tags = AccessSummaryFormatter.CalcTags(CalcAccess(viaProject: true, isPrivate: false), false).ToList();
        Assert.Contains(AccessSummaryFormatter.CalcTagViaProject, tags);
        Assert.DoesNotContain(AccessSummaryFormatter.CalcTagShared, tags);
        Assert.DoesNotContain(AccessSummaryFormatter.CalcTagLimited, tags);
    }
}
