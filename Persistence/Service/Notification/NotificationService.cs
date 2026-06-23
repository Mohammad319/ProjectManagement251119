using Application.Feature.Notification;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using Persistence.Service.Access;
using ProjectManagement.Shared.DTO.Notification;

namespace Persistence.Service.Notification
{
    public sealed class NotificationService(IDbContextFactoryTenant dbFactory) : INotificationService
    {
        public async Task<NotificationSummaryDTO> GetSummaryAsync(int userId, int take, CancellationToken ct = default)
        {
            if (userId <= 0)
                return new NotificationSummaryDTO();

            if (take <= 0) take = 15;

            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            // Deleted/hidden notifications never appear in the panel and are never counted.
            var query = ctx.Notifications.AsNoTracking().Where(n => n.UserId == userId && n.DeletedAt == null);

            var unreadCount = await query.CountAsync(n => n.ReadAt == null, ct);

            var items = await query
                .OrderBy(n => n.ReadAt != null)      // unread (ReadAt == null) first
                .ThenByDescending(n => n.CreatedAt)
                .Take(take)
                .Select(Projection)
                .ToListAsync(ct);

            return new NotificationSummaryDTO { UnreadCount = unreadCount, Items = items };
        }

        public async Task<IReadOnlyList<NotificationListItemDTO>> GetListAsync(int userId, int skip, int take, CancellationToken ct = default)
        {
            if (userId <= 0)
                return [];

            if (skip < 0) skip = 0;
            if (take <= 0) take = 30;

            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            return await ctx.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId && n.DeletedAt == null)
                .OrderBy(n => n.ReadAt != null)
                .ThenByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(Projection)
                .ToListAsync(ct);
        }

        public async Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default)
        {
            if (userId <= 0)
                return 0;

            await using var ctx = await dbFactory.CreateDbContextAsync(ct);
            return await ctx.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && n.ReadAt == null && n.DeletedAt == null, ct);
        }

        public async Task<bool> MarkAsReadAsync(int id, int userId, CancellationToken ct = default)
        {
            if (id <= 0 || userId <= 0)
                return false;

            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            var entity = await ctx.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);
            if (entity is null)
                return false;

            if (entity.ReadAt is null)
            {
                entity.MarkRead(DateTime.UtcNow);
                await ctx.SaveChangesAsync(ct);
            }

            return true;
        }

        public async Task<int> MarkAllAsReadAsync(int userId, CancellationToken ct = default)
        {
            if (userId <= 0)
                return 0;

            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            var unread = await ctx.Notifications
                .Where(n => n.UserId == userId && n.ReadAt == null && n.DeletedAt == null)
                .ToListAsync(ct);

            if (unread.Count == 0)
                return 0;

            var now = DateTime.UtcNow;
            foreach (var n in unread)
                n.MarkRead(now);

            await ctx.SaveChangesAsync(ct);
            return unread.Count;
        }

        public async Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default)
        {
            if (id <= 0 || userId <= 0)
                return false;

            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            var entity = await ctx.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);
            if (entity is null)
                return false;

            if (entity.DeletedAt is null)
            {
                entity.MarkDeleted(DateTime.UtcNow);
                await ctx.SaveChangesAsync(ct);
            }

            return true;
        }

        public async Task<int> ClearReadAsync(int userId, CancellationToken ct = default)
        {
            if (userId <= 0)
                return 0;

            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            var read = await ctx.Notifications
                .Where(n => n.UserId == userId && n.ReadAt != null && n.DeletedAt == null)
                .ToListAsync(ct);

            if (read.Count == 0)
                return 0;

            var now = DateTime.UtcNow;
            foreach (var n in read)
                n.MarkDeleted(now);

            await ctx.SaveChangesAsync(ct);
            return read.Count;
        }

        public async Task<ProjectOpenInfoDTO> GetProjectOpenInfoAsync(Guid projectId, int userId, int? departmentId, bool isViewer, CancellationToken ct = default)
        {
            if (projectId == Guid.Empty || userId <= 0)
                return new ProjectOpenInfoDTO { CanOpen = false };

            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            // Re-evaluate the live access rule; the global query filter already hides soft-deleted projects.
            var canSee = ProjectAccessRules.CanSee(userId, departmentId, isViewer);
            var info = await ctx.Projects.AsNoTracking()
                .Where(canSee)
                .Where(p => p.Id == projectId)
                .Select(p => new ProjectOpenInfoDTO
                {
                    CanOpen = true,
                    FolderId = p.FolderId,
                    DepartmentId = p.Folder.DepartmentId
                })
                .FirstOrDefaultAsync(ct);

            return info ?? new ProjectOpenInfoDTO { CanOpen = false };
        }

        private static System.Linq.Expressions.Expression<Func<Domain.Entities.Notifications.NotificationEntity, NotificationListItemDTO>> Projection =>
            n => new NotificationListItemDTO
            {
                Id = n.Id,
                Type = n.Type,
                ProjectId = n.ProjectId,
                CalculationId = n.CalculationId,
                ProjectName = n.ProjectName,
                ActorName = n.ActorName,
                DepartmentName = n.DepartmentName,
                Role = n.Role,
                CalcCount = n.CalcCount,
                CalcAllAvailable = n.CalcAllAvailable,
                ValidUntil = n.ValidUntil,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt
            };
    }
}
