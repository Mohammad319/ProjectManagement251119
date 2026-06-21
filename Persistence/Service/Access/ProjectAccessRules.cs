using System;
using System.Linq;
using System.Linq.Expressions;
using Domain.Entities.Project;
using ProjectManagement.Shared.Constant;

namespace Persistence.Service.Access
{
    /// <summary>
    /// Gemensam, återanvändbar åtkomstregel för vilka projekt en användare får se.
    /// Tillämpas som ett <c>.Where(...)</c>-predikat i alla projektqueries (lista, träd,
    /// sökning, rapporter, export) så att behörigheten alltid filtreras i backend.
    /// <para>
    /// - <b>Admin</b> (tenant-wide, <paramref name="departmentId"/> == null): ser alla projekt i tenanten.<br/>
    /// - <b>Visare</b> (<paramref name="isViewerOnly"/> == true): ser ENDAST projekt som delats med
    ///   användaren eller användarens avdelning (intern projektdelning).<br/>
    /// - <b>Övriga</b>: egen avdelning, egna skapade projekt, eller projekt som delats med dem.
    /// </para>
    /// </summary>
    public static class ProjectAccessRules
    {
        public static Expression<Func<ProjectEntity, bool>> CanSee(int userId, int? departmentId, bool isViewerOnly = false)
        {
            // Admin/tenant-wide ser allt (om inte rollen explicit begränsas till Visare).
            if (departmentId == null && !isViewerOnly)
                return _ => true;

            if (isViewerOnly)
                return p => p.Shares.Any(s =>
                    s.SharedWithUserId == userId ||
                    (departmentId != null && s.DepartmentId == departmentId));

            return p =>
                p.Folder.DepartmentId == departmentId ||
                p.CreatedBy == userId ||
                p.Shares.Any(s =>
                    s.SharedWithUserId == userId ||
                    (departmentId != null && s.DepartmentId == departmentId));
        }

        /// <summary>
        /// Reusable rule for who is allowed to EDIT (save grunddata of) a project. Used by
        /// <c>ProjectService.UpdateAsync</c> so the backend grants exactly the same effective
        /// write-permission the UI shows — closing the "shared as Användare but can't save" gap.
        /// <para>
        /// Effective edit permission = the LOWER of the user's system role and the share role:
        /// a project shared with role <b>Användare</b> (<see cref="PMRolesConst.Tenant.Manger"/>)
        /// grants edit; a share with role <b>Visare</b> never does. System Visare are blocked at the
        /// endpoint (write endpoints require <see cref="PMRolesConst.Tenant.AdminManger"/>), so a
        /// recipient reaching this rule is at least a system Användare and only the share role can
        /// down-grade them.
        /// </para>
        /// <list type="bullet">
        /// <item><b>Admin</b> (tenant-wide, <paramref name="departmentId"/> == null): edits everything.</item>
        /// <item><b>Own department</b> or <b>creator</b>: edits per the app's normal rules.</item>
        /// <item><b>Shared as Användare</b> (direct or via the user's department): may edit.</item>
        /// </list>
        /// </summary>
        public static Expression<Func<ProjectEntity, bool>> CanEdit(int userId, int? departmentId)
        {
            if (departmentId == null)
                return _ => true;

            return p =>
                p.Folder.DepartmentId == departmentId ||
                p.CreatedBy == userId ||
                p.Shares.Any(s =>
                    s.Role == PMRolesConst.Tenant.Manger &&
                    (s.SharedWithUserId == userId || s.DepartmentId == departmentId));
        }
    }
}
