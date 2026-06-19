using System;
using System.Linq;
using System.Linq.Expressions;
using Domain.Entities.Calculation;

namespace Persistence.Service.Access
{
    /// <summary>
    /// Gemensam, återanvändbar åtkomstregel för vilka kalkyler en användare får se.
    /// Tillämpas som ett <c>.Where(...)</c>-predikat i kalkylqueries (lista, detalj, sökning,
    /// rapporter, export) så att privata och odelade kalkyler aldrig läcker i backend.
    /// <para>
    /// - <b>Privata kalkyler</b> visas bara för skaparen/ägaren och för Admin (tenant-wide, departmentId == null).<br/>
    /// - <b>Visare</b> (<paramref name="isViewerOnly"/>): ser ENDAST kalkyler som valts i en projektdelning
    ///   till användaren/avdelningen, och aldrig privata kalkyler.<br/>
    /// - <b>Övriga</b>: kalkyler i projekt de har åtkomst till (egen avdelning, egna skapade, eller delade
    ///   projekt), med privat-regeln ovan.
    /// </para>
    /// </summary>
    public static class CalculationAccessRules
    {
        public static Expression<Func<CalculationEntity, bool>> CanSee(int userId, int? departmentId, bool isViewerOnly = false)
        {
            if (isViewerOnly)
            {
                // Visare: bara kalkyler som uttryckligen valts i en projektdelning – aldrig privata.
                return c => !c.IsPrivate && c.Project.Shares.Any(s =>
                    (s.SharedWithUserId == userId || (departmentId != null && s.DepartmentId == departmentId)) &&
                    s.Calculations.Any(sc => sc.CalculationId == c.Id));
            }

            return c =>
                (
                    departmentId == null ||
                    c.Project.Folder.DepartmentId == departmentId ||
                    c.CreatedBy == userId ||
                    c.Project.Shares.Any(s =>
                        s.SharedWithUserId == userId ||
                        (departmentId != null && s.DepartmentId == departmentId))
                )
                &&
                (
                    // Privat-regeln: bara ägare/skapare eller Admin (tenant-wide).
                    !c.IsPrivate || c.CreatedBy == userId || departmentId == null
                );
        }
    }
}
