using Application.Feature.Calculation.ReviewerComment;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.Factory;
using Persistence.Service.Access;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;

namespace Persistence.Service.CalculationItems.ReviewerComment
{
    /// <summary>
    /// Saves a reviewer comment (Granskarkommentar) on a calculation row (task/resource) in isolation
    /// from the economy. Allowed even when the calculation is locked – economic fields are never touched
    /// here, so writing a comment never unlocks the calculation nor changes its economic result.
    /// Access is enforced in the backend via <see cref="CalculationAccessRules"/> (a Viewer/reviewer
    /// only sees shared/selected calculations, never private ones).
    /// </summary>
    public class ReviewerCommentService(
        IDbContextFactoryTenant dbFactory,
        ILogger<ReviewerCommentService> logger) : IReviewerCommentService
    {
        public async Task<bool> SaveAsync(ReviewerCommentSaveDTO dto, int userId, int? departmentId, bool isViewer, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // Resolve the calculation the row belongs to (task directly, resource via its task).
            int? calcId = dto.ItemType == CalculationItemType.task
                ? await context.Tasks.Where(t => t.Id == dto.ItemId).Select(t => (int?)t.CalculationId).FirstOrDefaultAsync(ct)
                : await context.Resources.Where(r => r.Id == dto.ItemId).Select(r => (int?)r.Task.CalculationId).FirstOrDefaultAsync(ct);

            if (calcId is null)
                return false;

            // Backend access control – not just UI. A Viewer/reviewer must be able to see the calculation.
            // This intentionally allows commenting even on a locked calculation (only the comment is written).
            var allowed = await context.Calculations
                .Where(CalculationAccessRules.CanSee(userId, departmentId, isViewer))
                .AnyAsync(c => c.Id == calcId.Value, ct);

            if (!allowed)
                return false;

            bool changed;
            string action;

            if (dto.ItemType == CalculationItemType.task)
            {
                var task = await context.Tasks.FirstOrDefaultAsync(t => t.Id == dto.ItemId, ct);
                if (task is null) return false;
                action = ResolveAction(task.ReviewerComment, dto.Text);
                changed = task.SetReviewerComment(dto.Text);
            }
            else
            {
                var res = await context.Resources.FirstOrDefaultAsync(r => r.Id == dto.ItemId, ct);
                if (res is null) return false;
                action = ResolveAction(res.ReviewerComment, dto.Text);
                changed = res.SetReviewerComment(dto.Text);
            }

            if (!changed)
                return true;

            await context.SaveChangesAsync(ct);

            // Logging: reviewer comment created/changed/deleted – calculation, row, user, time.
            logger.LogInformation(
                "Granskarkommentar {Action}: {ItemType} #{ItemId} i kalkyl #{CalcId} av användare #{UserId} {Time:o}.",
                action, dto.ItemType, dto.ItemId, calcId.Value, userId, DateTime.UtcNow);

            return true;
        }

        private static string ResolveAction(string? current, string? next)
        {
            var hadValue = !string.IsNullOrWhiteSpace(current);
            var hasValue = !string.IsNullOrWhiteSpace(next);
            if (!hadValue && hasValue) return "skapad";
            if (hadValue && !hasValue) return "raderad";
            return "ändrad";
        }
    }
}
