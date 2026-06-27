using System.Security.Claims;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Helper;

public readonly record struct ProjectContextMenuAccess(
    bool CanView,
    bool CanEditWork,
    bool CanManageLifecycle,
    bool CanManageSharing);

public readonly record struct CalculationContextMenuAccess(
    bool CanView,
    bool CanEditWork,
    bool CanManageLifecycle);

public static class ContextMenuAccessPolicy
{
    public static ProjectContextMenuAccess ForProject(
        ClaimsPrincipal user,
        ProjectAccessSummaryDTO? access,
        bool otherDepartment)
    {
        var isEditor = IsInAnyRole(user, PMRolesConst.Tenant.AdminManger);
        var normalAccess = access?.ViaDepartment ?? true;
        var shareEdit = MatchingShareGrantsEdit(user, access?.Recipients);

        var canEditWork = isEditor && (normalAccess || shareEdit);
        var canManage = !otherDepartment && isEditor && normalAccess;

        return new ProjectContextMenuAccess(
            CanView: user.Identity?.IsAuthenticated == true,
            CanEditWork: canEditWork,
            CanManageLifecycle: canManage,
            CanManageSharing: canManage);
    }

    public static CalculationContextMenuAccess ForCalculation(
        ClaimsPrincipal user,
        CalculationAccessSummaryDTO? access,
        bool otherDepartment)
    {
        var isEditor = IsInAnyRole(user, PMRolesConst.Tenant.AdminManger);
        var normalAccess = access?.ViaProject ?? true;
        var shareEdit = MatchingShareGrantsEdit(user, access?.Recipients);

        return new CalculationContextMenuAccess(
            CanView: user.Identity?.IsAuthenticated == true,
            CanEditWork: isEditor && (normalAccess || shareEdit),
            CanManageLifecycle: !otherDepartment && isEditor && normalAccess);
    }

    private static bool MatchingShareGrantsEdit(
        ClaimsPrincipal user,
        IEnumerable<ProjectAccessRecipientDTO>? recipients)
        => recipients?.Any(r => RecipientMatchesUser(user, r) && r.Role == PMRolesConst.Tenant.Manger) == true;

    private static bool RecipientMatchesUser(ClaimsPrincipal user, ProjectAccessRecipientDTO recipient)
    {
        var userId = TryGetIntClaim(user, PMClaimsConst.UserId);
        var departmentId = TryGetIntClaim(user, PMClaimsConst.DepartmentId);

        return (recipient.UserId.HasValue && userId == recipient.UserId.Value)
            || (recipient.DepartmentId.HasValue && departmentId == recipient.DepartmentId.Value);
    }

    private static bool IsInAnyRole(ClaimsPrincipal user, string roles)
        => roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(user.IsInRole);

    private static int? TryGetIntClaim(ClaimsPrincipal user, string claimType)
        => int.TryParse(user.FindFirst(claimType)?.Value, out var value) ? value : null;
}
