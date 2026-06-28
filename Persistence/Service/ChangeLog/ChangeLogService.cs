using Application.Feature.ChangeLog;
using Domain.Entities.ChangeLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.ChangeLog;
using ProjectManagement.Shared.Enums;

namespace Persistence.Service.ChangeLog
{
    /// <summary>
    /// Records coarse project/calculation changes and reads back the latest N per object for the
    /// "Senaste ändringar" indicator tooltip. Writes are best-effort: a failure is logged and
    /// swallowed so it never breaks the originating operation. The actor (CreatedBy/TenantId/CreatedAt)
    /// is filled by the audit interceptor from the request context; the display name is denormalized.
    /// </summary>
    public sealed class ChangeLogService(IDbContextFactoryTenant dbFactory, ILogger<ChangeLogService> logger)
        : IChangeLogService
    {
        // A single read fetches at most this many rows for the requested ids (newest first). The
        // indicator only asks for a handful of just-changed objects, so this is a generous safety cap.
        private const int MaxRowsPerRead = 500;

        public Task AppendProjectAsync(Guid projectId, ChangeAction action, int actorUserId, CancellationToken ct = default)
            => AppendAsync(isProject: true, projectId, calculationId: 0, action, actorUserId, ct);

        public Task AppendCalculationAsync(int calculationId, ChangeAction action, int actorUserId, CancellationToken ct = default)
            => AppendAsync(isProject: false, projectId: Guid.Empty, calculationId, action, actorUserId, ct);

        private async Task AppendAsync(bool isProject, Guid projectId, int calculationId, ChangeAction action, int actorUserId, CancellationToken ct)
        {
            try
            {
                await using var ctx = await dbFactory.CreateDbContextAsync(ct);

                var actorName = await ResolveActorNameAsync(ctx, actorUserId, ct);

                var entry = isProject
                    ? ChangeLogEntity.ForProject(projectId, action, actorName)
                    : ChangeLogEntity.ForCalculation(calculationId, action, actorName);

                ctx.ChangeLogs.Add(entry);
                await ctx.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                // Best-effort: the change log must never break the originating create/update/share.
                logger.LogWarning(ex,
                    "Failed to append change-log entry. IsProject={IsProject} ProjectId={ProjectId} CalculationId={CalculationId} Action={Action}",
                    isProject, projectId, calculationId, action);
            }
        }

        public async Task<IReadOnlyDictionary<Guid, List<ChangeLogItemDTO>>> GetRecentForProjectsAsync(
            IEnumerable<Guid> projectIds, int take, CancellationToken ct = default)
        {
            var ids = projectIds.Distinct().Where(id => id != Guid.Empty).ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, List<ChangeLogItemDTO>>();

            if (take <= 0) take = 5;

            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            var rows = await ctx.ChangeLogs
                .AsNoTracking()
                .Where(x => x.ProjectId != null && ids.Contains(x.ProjectId.Value))
                .OrderByDescending(x => x.CreatedAt)
                .Take(MaxRowsPerRead)
                .Select(x => new { Key = x.ProjectId!.Value, x.CreatedAt, x.ActorName, x.Action })
                .ToListAsync(ct);

            return rows
                .GroupBy(x => x.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.Take(take).Select(x => new ChangeLogItemDTO
                    {
                        CreatedAt = x.CreatedAt,
                        ActorName = x.ActorName,
                        Action = x.Action
                    }).ToList());
        }

        public async Task<IReadOnlyDictionary<int, List<ChangeLogItemDTO>>> GetRecentForCalculationsAsync(
            IEnumerable<int> calculationIds, int take, CancellationToken ct = default)
        {
            var ids = calculationIds.Distinct().Where(id => id > 0).ToList();
            if (ids.Count == 0)
                return new Dictionary<int, List<ChangeLogItemDTO>>();

            if (take <= 0) take = 5;

            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            var rows = await ctx.ChangeLogs
                .AsNoTracking()
                .Where(x => x.CalculationId != null && ids.Contains(x.CalculationId.Value))
                .OrderByDescending(x => x.CreatedAt)
                .Take(MaxRowsPerRead)
                .Select(x => new { Key = x.CalculationId!.Value, x.CreatedAt, x.ActorName, x.Action })
                .ToListAsync(ct);

            return rows
                .GroupBy(x => x.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.Take(take).Select(x => new ChangeLogItemDTO
                    {
                        CreatedAt = x.CreatedAt,
                        ActorName = x.ActorName,
                        Action = x.Action
                    }).ToList());
        }

        private static async Task<string?> ResolveActorNameAsync(Persistence.Context.ShardingSingleDbContext ctx, int actorUserId, CancellationToken ct)
        {
            if (actorUserId <= 0)
                return null;

            return await ctx.User
                .AsNoTracking()
                .Where(u => u.Id == actorUserId)
                .Select(u => ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim() == ""
                    ? u.UserName
                    : ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim())
                .FirstOrDefaultAsync(ct);
        }
    }
}
