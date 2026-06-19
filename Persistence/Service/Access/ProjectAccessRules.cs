using System;
using System.Linq;
using System.Linq.Expressions;
using Domain.Entities.Project;

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
    }
}
