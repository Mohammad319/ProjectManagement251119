using Application.Feature.Calculation.ProductionNote;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.Factory;
using Persistence.Service.Access;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;

namespace Persistence.Service.CalculationItems.ProductionNote
{
    /// <summary>
    /// Saves a production note on a calculation row (task/resource) in isolation from the economy.
    /// Allowed even when the calculation is locked – economic fields are never touched here.
    /// Access is enforced in the backend via <see cref="CalculationAccessRules"/> (Viewer only sees
    /// shared/selected calculations, never private ones).
    /// </summary>
    public class ProductionNoteService(
        IDbContextFactoryTenant dbFactory,
        ILogger<ProductionNoteService> logger) : IProductionNoteService
    {
        public async Task<bool> SaveAsync(ProductionNoteSaveDTO dto, int userId, int? departmentId, bool isViewer, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // Resolve the calculation the row belongs to (task directly, resource via its task).
            int? calcId = dto.ItemType == CalculationItemType.task
                ? await context.Tasks.Where(t => t.Id == dto.ItemId).Select(t => (int?)t.CalculationId).FirstOrDefaultAsync(ct)
                : await context.Resources.Where(r => r.Id == dto.ItemId).Select(r => (int?)r.Task.CalculationId).FirstOrDefaultAsync(ct);

            if (calcId is null)
                return false;

            // Backend access control – not just UI. Viewer must have the calculation shared/selected.
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
                action = ResolveAction(task.ProductionNote, dto.Text);
                changed = task.SetProductionNote(dto.Text);
            }
            else
            {
                var res = await context.Resources.FirstOrDefaultAsync(r => r.Id == dto.ItemId, ct);
                if (res is null) return false;
                action = ResolveAction(res.ProductionNote, dto.Text);
                changed = res.SetProductionNote(dto.Text);
            }

            if (!changed)
                return true;

            await context.SaveChangesAsync(ct);

            // Logging: production note created/changed/deleted – calculation, row, user, time.
            logger.LogInformation(
                "Produktionsanteckning {Action}: {ItemType} #{ItemId} i kalkyl #{CalcId} av användare #{UserId} {Time:o}.",
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
