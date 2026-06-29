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
        /// <summary>User-facing error text when a lifecycle action is blocked (UI also hides it).</summary>
        public const string LifecycleForbiddenMessage = "Du saknar behörighet att utföra denna kalkylåtgärd.";

        /// <summary>
        /// Reusable rule for who may perform ADMINISTRATIVE LIFECYCLE actions on a calculation —
        /// move, copy, archive, export, delete and version management. This is intentionally stricter
        /// than <see cref="CanSee"/>/<see cref="CanEdit"/>: access gained via an EXTRA project share
        /// (even "Kan ändra") grants work access to the calc's content but never lifecycle management.
        /// <para>
        /// Allowed for Admin (tenant-wide, <paramref name="departmentId"/> == null) and for users with
        /// NORMAL access to the calc's own department (<c>c.DepartmentId == departmentId</c>) — the same
        /// predicate the move/copy/delete/version services already apply, extracted here so archive and
        /// export enforce it too.
        /// </para>
        /// </summary>
        public static Expression<Func<CalculationEntity, bool>> CanManageLifecycle(int? departmentId)
        {
            if (departmentId == null)
                return _ => true;

            return c => c.DepartmentId == departmentId;
        }

        /// <summary>True when the user may perform lifecycle actions on calculation <paramref name="calcId"/>.</summary>
        public static Task<bool> CanManageLifecycleAsync(
            IQueryable<CalculationEntity> calculations,
            int calcId,
            int? departmentId,
            CancellationToken ct = default)
            => calculations.Where(CanManageLifecycle(departmentId)).AnyAsync(c => c.Id == calcId, ct);

        public static Expression<Func<CalculationEntity, bool>> CanSee(int userId, int? departmentId, bool isViewerOnly = false)
        {
            var today = DateTime.UtcNow.Date;

            if (isViewerOnly)
            {
                // Visare: bara kalkyler som ingår i en projektdelning – aldrig privata. Delningen kan
                // omfatta antingen ALLA icke-privata kalkyler (s.AllCalculations) eller bara de valda.
                return c => !c.IsPrivate && c.Project.Shares.Any(s =>
                    (s.ValidUntil == null || s.ValidUntil >= today) &&
                    (s.SharedWithUserId == userId || (departmentId != null && s.DepartmentId == departmentId)) &&
                    (s.AllCalculations || s.Calculations.Any(sc => sc.CalculationId == c.Id)));
            }

            return c =>
                (
                    departmentId == null ||
                    c.Project.Folder.DepartmentId == departmentId ||
                    c.CreatedBy == userId ||
                    // Delning: "Alla kalkyler i projektet" (s.AllCalculations) inkluderar automatiskt nya
                    // icke-privata kalkyler; annars bara de uttryckligen valda. Privat-regeln nedan (AND)
                    // ser till att privata kalkyler aldrig läcker via "alla"-läget.
                    c.Project.Shares.Any(s =>
                        (s.ValidUntil == null || s.ValidUntil >= today) &&
                        (s.SharedWithUserId == userId ||
                         (departmentId != null && s.DepartmentId == departmentId)) &&
                        (s.AllCalculations || s.Calculations.Any(sc => sc.CalculationId == c.Id)))
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
            var today = DateTime.UtcNow.Date;

            if (departmentId == null)
                return _ => true;

            return c =>
                c.DepartmentId == departmentId ||
                c.CreatedBy == userId ||
                // Delning som "Kan ändra": "Alla kalkyler i projektet" inkluderar nya icke-privata kalkyler,
                // annars bara de valda. Privata kalkyler får ALDRIG redigeras via en delning (!c.IsPrivate).
                (!c.IsPrivate && c.Project.Shares.Any(s =>
                    (s.ValidUntil == null || s.ValidUntil >= today) &&
                    s.Role == PMRolesConst.Tenant.Manger &&
                    (s.SharedWithUserId == userId || s.DepartmentId == departmentId) &&
                    (s.AllCalculations || s.Calculations.Any(sc => sc.CalculationId == c.Id))));
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
