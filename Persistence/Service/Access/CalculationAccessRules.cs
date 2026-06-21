using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Constant;

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

        /// <summary>
        /// Reusable rule for who may EDIT (save) a calculation. Used by
        /// <c>CalculationService.UpdateAsync</c> so the backend grants the same effective write
        /// permission the UI shows — the calc analogue of <c>ProjectAccessRules.CanEdit</c>.
        /// <para>
        /// Effective edit permission is capped by the system role; a calc is editable when the
        /// user is Admin (tenant-wide), owns the calc's department, created it, OR the calc's
        /// project is shared with them (direct or via department) with role <b>Användare</b>
        /// (<see cref="PMRolesConst.Tenant.Manger"/>) AND the calc is one of the selected shared
        /// calculations. A Visare-share never grants edit; system Visare are blocked at the
        /// write endpoint (<see cref="PMRolesConst.Tenant.AdminManger"/>).
        /// </para>
        /// </summary>
        public static Expression<Func<CalculationEntity, bool>> CanEdit(int userId, int? departmentId)
        {
            if (departmentId == null)
                return _ => true;

            return c =>
                c.DepartmentId == departmentId ||
                c.CreatedBy == userId ||
                c.Project.Shares.Any(s =>
                    s.Role == PMRolesConst.Tenant.Manger &&
                    (s.SharedWithUserId == userId || s.DepartmentId == departmentId) &&
                    s.Calculations.Any(sc => sc.CalculationId == c.Id));
        }

        /// <summary>
        /// True when the user may edit the calculation <paramref name="calcId"/>. Reused by the
        /// resource/task write services so a grid edit on a calc shared as "Användare" is accepted
        /// by the backend — the same effective-permission rule the UI applies.
        /// </summary>
        public static Task<bool> CanEditCalcAsync(
            IQueryable<CalculationEntity> calculations,
            int calcId,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
            => calculations.Where(CanEdit(userId, departmentId)).AnyAsync(c => c.Id == calcId, ct);
    }
}
