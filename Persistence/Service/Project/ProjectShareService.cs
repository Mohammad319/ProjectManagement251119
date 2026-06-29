using Application.Feature.Project.ProjectShare;
using Application.Interfaces;
using Domain.Entities.Notifications;
using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Factory;
using Persistence.Service.Notification;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Exceptions;

namespace Persistence.Service.Project
{
    public sealed class ProjectShareService(
        IDbContextFactoryTenant dbFactory,
        INotificationPublisher? publisher = null,
        IUserSystemRoleProvider? roleProvider = null,
        global::Application.Feature.ChangeLog.IChangeLogService? changeLog = null) : IProjectShareService
    {
        public async Task<IReadOnlyList<ProjectShareListItemDTO>> GetByProjectAsync(
            Guid projectId, int? departmentId, int userId, CancellationToken ct = default)
        {
            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            return await ctx.ProjectShare
                .AsNoTracking()
                .Where(s => s.ProjectId == projectId)
                .Select(s => new ProjectShareListItemDTO
                {
                    Id = s.Id,
                    RecipientType = s.SharedWithUserId != null
                        ? ProjectShareRecipientType.User
                        : ProjectShareRecipientType.Department,
                    UserId = s.SharedWithUserId,
                    DepartmentId = s.DepartmentId,
                    RecipientName = s.SharedWithUserId != null
                        ? (((s.SharedWithUser!.FirstName ?? "") + " " + (s.SharedWithUser!.LastName ?? "")).Trim() == ""
                            ? s.SharedWithUser!.UserName
                            : ((s.SharedWithUser!.FirstName ?? "") + " " + (s.SharedWithUser!.LastName ?? "")).Trim())
                        : s.Department!.Name,
                    Role = s.Role,
                    ValidUntil = s.ValidUntil,
                    AllCalculations = s.AllCalculations,
                    CalculationIds = s.Calculations.Select(c => c.CalculationId).ToList(),
                    // "Delad av" + tooltip-metadata (vem som skapade/ändrade delningen).
                    CreatedByName = s.CreatedByUser != null
                        ? (((s.CreatedByUser.FirstName ?? "") + " " + (s.CreatedByUser.LastName ?? "")).Trim() == ""
                            ? s.CreatedByUser.UserName
                            : ((s.CreatedByUser.FirstName ?? "") + " " + (s.CreatedByUser.LastName ?? "")).Trim())
                        : null,
                    CreatedAt = s.CreatedAt,
                    UpdatedByName = s.UpdatedByUser != null
                        ? (((s.UpdatedByUser.FirstName ?? "") + " " + (s.UpdatedByUser.LastName ?? "")).Trim() == ""
                            ? s.UpdatedByUser.UserName
                            : ((s.UpdatedByUser.FirstName ?? "") + " " + (s.UpdatedByUser.LastName ?? "")).Trim())
                        : null,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync(ct);
        }

        public async Task<int> UpsertAsync(
            Guid projectId, ProjectShareUpsertDTO dto, int userId, int? departmentId, CancellationToken ct = default)
        {
            if (dto is null)
                return 0;

            // Exactly one recipient must be set according to the selected type.
            bool validUser = dto.RecipientType == ProjectShareRecipientType.User && dto.UserId is > 0 && dto.DepartmentId is null;
            bool validDept = dto.RecipientType == ProjectShareRecipientType.Department && dto.DepartmentId is > 0 && dto.UserId is null;
            if (!validUser && !validDept)
                return 0;

            var validUntil = dto.ValidUntil?.Date;
            if (validUntil.HasValue && validUntil.Value < DateTime.UtcNow.Date)
                return 0;

            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            // The project must exist within the current tenant (global query filter).
            var projectName = await ctx.Projects
                .Where(p => p.Id == projectId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct);
            if (projectName is null)
                return 0;

            // Sharing is administrative: receiving a project via an extra share never grants the right
            // to (re-)share it. Only Admin/own-department/creator may manage sharing.
            if (!await Access.ProjectAccessRules.CanManageLifecycleAsync(ctx.Projects, projectId, userId, departmentId, ct))
                throw new ForbiddenActionException(Access.ProjectAccessRules.ShareForbiddenMessage);

            ProjectShareEntity? entity = dto.Id is > 0
                ? await ctx.ProjectShare
                    .Include(s => s.Calculations)
                    .FirstOrDefaultAsync(s => s.Id == dto.Id && s.ProjectId == projectId, ct)
                : null;

            bool isNew = entity is null;

            // Capture the previous state before mutating, so we can classify the change for notifications.
            string? oldRole = entity?.Role;
            DateTime? oldValid = entity?.ValidUntil;
            bool oldAll = entity?.AllCalculations ?? false;
            var oldCalcIds = entity?.Calculations.Select(c => c.CalculationId).ToList() ?? [];

            // "Alla kalkyler i projektet": kalkyllistan ignoreras av åtkomstreglerna; rensa den så att
            // ingen vilseledande explicit lista sparas. "Valda kalkyler": spara de valda (privata redan
            // bortfiltrerade av anroparen).
            var calcIdsToStore = dto.AllCalculations ? Enumerable.Empty<int>() : dto.CalculationIds;

            if (entity is null)
            {
                entity = validUser
                    ? ProjectShareEntity.ForUser(projectId, dto.UserId!.Value, dto.Role)
                    : ProjectShareEntity.ForDepartment(projectId, dto.DepartmentId!.Value, dto.Role);
                entity.SetAllCalculations(dto.AllCalculations);
                entity.ReplaceCalculations(calcIdsToStore);
                entity.SetValidUntil(validUntil);
                ctx.ProjectShare.Add(entity);
            }
            else
            {
                entity.SetRole(dto.Role);
                entity.SetValidUntil(validUntil);
                entity.SetAllCalculations(dto.AllCalculations);
                entity.ReplaceCalculations(calcIdsToStore);
            }

            var notifications = await BuildUpsertNotificationsAsync(
                ctx, projectId, projectName, dto, validUser, validUntil, isNew, oldRole, oldValid, oldAll, oldCalcIds, userId, ct);

            foreach (var n in notifications)
                ctx.Notifications.Add(n);

            await ctx.SaveChangesAsync(ct);

            await PublishAsync(notifications.Select(n => n.UserId), ct);

            // Record the share so the project's change indicator can show "Delade projektet".
            if (isNew && changeLog is not null)
                await changeLog.AppendProjectAsync(projectId, ChangeAction.Shared, userId, ct);

            return entity.Id;
        }

        public async Task<bool> DeleteAsync(int id, int userId, int? departmentId, CancellationToken ct = default)
        {
            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            var entity = await ctx.ProjectShare.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (entity is null)
                return false;

            var projectId = entity.ProjectId;

            // Only Admin/own-department/creator may manage sharing — not someone who merely received
            // the project via an extra share.
            if (!await Access.ProjectAccessRules.CanManageLifecycleAsync(ctx.Projects, projectId, userId, departmentId, ct))
                throw new ForbiddenActionException(Access.ProjectAccessRules.ShareForbiddenMessage);
            var projectName = await ctx.Projects
                .Where(p => p.Id == projectId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct) ?? string.Empty;

            // Determine who loses access BEFORE removing the row.
            var affected = await ResolveRemovalRecipientsAsync(ctx, entity, projectId, userId, ct);

            ctx.ProjectShare.Remove(entity);

            foreach (var uid in affected)
                ctx.Notifications.Add(ProjectShareNotificationFactory.AccessRemoved(uid, projectId, projectName));

            await ctx.SaveChangesAsync(ct);

            await PublishAsync(affected, ct);

            return true;
        }

        // ── Notification helpers ─────────────────────────────────────────────

        private async Task<List<NotificationEntity>> BuildUpsertNotificationsAsync(
            ShardingSingleDbContext ctx, Guid projectId, string projectName, ProjectShareUpsertDTO dto,
            bool validUser, DateTime? newValid, bool isNew, string? oldRole, DateTime? oldValid,
            bool oldAll, List<int> oldCalcIds, int actorId, CancellationToken ct)
        {
            var result = new List<NotificationEntity>();

            var newRole = dto.Role;
            var newCalcIds = (dto.CalculationIds ?? []).Distinct().ToList();

            // Antal icke-privata (delbara) kalkyler – nämnaren och "alla kalkyler"-texten i aviseringar.
            var shareableCalcs = await ctx.Calculations.CountAsync(c => c.ProjectId == projectId && !c.IsPrivate, ct);

            // "Alla kalkyler i projektet" → räkna som alla delbara; annars antalet valda.
            int calcCount = dto.AllCalculations ? shareableCalcs : newCalcIds.Count;
            bool calcAll = dto.AllCalculations || (calcCount > 0 && shareableCalcs > 0 && calcCount >= shareableCalcs);

            // Change classification (only relevant for updates). Ett byte av delningsomfattning
            // (valda ↔ alla) räknas som en kalkyländring även om id-listan är oförändrad.
            bool roleChanged = !string.Equals(oldRole, newRole, StringComparison.Ordinal);
            bool validChanged = oldValid != newValid;
            bool calcsChanged = oldAll != dto.AllCalculations || !oldCalcIds.ToHashSet().SetEquals(newCalcIds);

            NotificationEntity? UpdateNotif(int uid, string role)
            {
                int changes = (roleChanged ? 1 : 0) + (validChanged ? 1 : 0) + (calcsChanged ? 1 : 0);
                if (changes == 0)
                    return null;

                // Several changes at once → a single aggregated notification (avoid notification spam).
                if (changes > 1 || roleChanged)
                    return ProjectShareNotificationFactory.AccessChanged(uid, projectId, projectName, role, calcCount, calcAll, newValid);
                if (validChanged)
                    return ProjectShareNotificationFactory.ValidityChanged(uid, projectId, projectName, role, calcCount, calcAll, newValid);
                return ProjectShareNotificationFactory.CalculationsChanged(uid, projectId, projectName, role, calcCount, calcAll, newValid);
            }

            if (validUser)
            {
                int recipientId = dto.UserId!.Value;
                if (recipientId == actorId)
                    return result; // never notify yourself

                // System Visare can only ever "view", even if shared as edit.
                var viewers = await ResolveViewerUserIdsAsync(ctx, [recipientId], ct);
                var effRole = viewers.Contains(recipientId) ? PMRolesConst.Tenant.Viewer : newRole;

                if (isNew)
                {
                    var actorName = await GetUserDisplayNameAsync(ctx, actorId, ct);
                    result.Add(ProjectShareNotificationFactory.Shared(
                        recipientId, projectId, projectName, actorName, null, effRole, calcCount, calcAll, newValid, viaDepartment: false));
                }
                else if (UpdateNotif(recipientId, effRole) is { } notif)
                {
                    result.Add(notif);
                }

                return result;
            }

            // Department share → fan out to active members who gain/keep access through this share.
            int deptId = dto.DepartmentId!.Value;
            var deptName = await ctx.Department
                .Where(d => d.Id == deptId)
                .Select(d => d.Name)
                .FirstOrDefaultAsync(ct) ?? string.Empty;

            var deptUserIds = await ctx.User
                .Where(u => u.DepartmentId == deptId)
                .Select(u => u.Id)
                .ToListAsync(ct);

            // Skip users who already have an equal/stronger DIRECT share for this project (no duplicate notice).
            int newRank = ProjectShareNotificationFactory.RoleRank(newRole);
            var directShares = await ctx.ProjectShare
                .Where(s => s.ProjectId == projectId && s.SharedWithUserId != null)
                .Select(s => new { UserId = s.SharedWithUserId!.Value, s.Role })
                .ToListAsync(ct);
            var strongerDirect = directShares
                .Where(d => ProjectShareNotificationFactory.RoleRank(d.Role) >= newRank)
                .Select(d => d.UserId)
                .ToHashSet();

            var candidates = deptUserIds.Where(uid => uid != actorId && !strongerDirect.Contains(uid)).ToList();
            var viewerIds = await ResolveViewerUserIdsAsync(ctx, candidates, ct);

            foreach (var uid in candidates)
            {
                var effRole = viewerIds.Contains(uid) ? PMRolesConst.Tenant.Viewer : newRole;

                if (isNew)
                    result.Add(ProjectShareNotificationFactory.Shared(
                        uid, projectId, projectName, null, deptName, effRole, calcCount, calcAll, newValid, viaDepartment: true));
                else if (UpdateNotif(uid, effRole) is { } notif)
                    result.Add(notif);
            }

            return result;
        }

        /// <summary>Of the given users, those who are system Visare (Viewer). Empty when no role provider is wired.</summary>
        private async Task<HashSet<int>> ResolveViewerUserIdsAsync(ShardingSingleDbContext ctx, IEnumerable<int> userIds, CancellationToken ct)
        {
            if (roleProvider is null)
                return [];

            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
                return [];

            var map = await ctx.User
                .Where(u => ids.Contains(u.Id) && u.ExternalAuthId != "")
                .Select(u => new { u.Id, u.ExternalAuthId })
                .ToListAsync(ct);

            var idByAuth = map.ToDictionary(m => m.ExternalAuthId, m => m.Id, StringComparer.Ordinal);
            if (idByAuth.Count == 0)
                return [];

            var viewerAuthIds = await roleProvider.GetViewerAuthIdsAsync(idByAuth.Keys, ct);
            return viewerAuthIds.Where(idByAuth.ContainsKey).Select(a => idByAuth[a]).ToHashSet();
        }

        private static async Task<List<int>> ResolveRemovalRecipientsAsync(
            ShardingSingleDbContext ctx, ProjectShareEntity entity, Guid projectId, int actorId, CancellationToken ct)
        {
            List<int> affected;

            if (entity.SharedWithUserId is { } directUser)
            {
                affected = [directUser];
            }
            else
            {
                int deptId = entity.DepartmentId!.Value;
                var deptUsers = await ctx.User
                    .Where(u => u.DepartmentId == deptId)
                    .Select(u => u.Id)
                    .ToListAsync(ct);

                // Members who still hold a direct share keep access and should not be told it was removed.
                var directUserIds = (await ctx.ProjectShare
                    .Where(s => s.ProjectId == projectId && s.SharedWithUserId != null && s.Id != entity.Id)
                    .Select(s => s.SharedWithUserId!.Value)
                    .ToListAsync(ct)).ToHashSet();

                affected = deptUsers.Where(u => !directUserIds.Contains(u)).ToList();
            }

            return affected.Where(u => u != actorId).Distinct().ToList();
        }

        private static async Task<string> GetUserDisplayNameAsync(ShardingSingleDbContext ctx, int userId, CancellationToken ct)
        {
            var u = await ctx.User
                .Where(x => x.Id == userId)
                .Select(x => new { x.FirstName, x.LastName, x.UserName })
                .FirstOrDefaultAsync(ct);

            if (u is null)
                return string.Empty;

            var full = $"{u.FirstName} {u.LastName}".Trim();
            return string.IsNullOrWhiteSpace(full) ? u.UserName : full;
        }

        private async Task PublishAsync(IEnumerable<int> userIds, CancellationToken ct)
        {
            if (publisher is null)
                return;

            var ids = userIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0)
                return;

            try
            {
                await publisher.PublishUnreadChangedAsync(ids, ct);
            }
            catch
            {
                // Realtime push is best-effort; failure must not affect the share operation.
            }
        }
    }
}
