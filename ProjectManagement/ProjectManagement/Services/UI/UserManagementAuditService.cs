using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;

namespace ProjectManagement.Services.UI;

public sealed record UserManagementAuditItem(
    long Id,
    string Action,
    int? TargetUserId,
    string TargetDisplayName,
    string ActorDisplayName,
    string? Details,
    bool Succeeded,
    DateTimeOffset CreatedAtUtc);

public interface IUserManagementAuditService
{
    Task WriteAsync(
        string action,
        int? targetUserId,
        string? targetAuthId,
        string? targetDisplayName,
        string? details = null,
        bool succeeded = true,
        CancellationToken ct = default);

    Task<IReadOnlyList<UserManagementAuditItem>> GetRecentAsync(int take = 50, CancellationToken ct = default);
}

public sealed class UserManagementAuditService(
    IDbContextFactoryTenant dbFactory,
    ITenantContext tenantContext,
    ILogger<UserManagementAuditService> logger) : IUserManagementAuditService
{
    public async Task WriteAsync(
        string action,
        int? targetUserId,
        string? targetAuthId,
        string? targetDisplayName,
        string? details = null,
        bool succeeded = true,
        CancellationToken ct = default)
    {
        try
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var actorName = "System";

            if (tenantContext.UserId is > 0)
            {
                actorName = await context.User
                    .AsNoTracking()
                    .Where(x => x.Id == tenantContext.UserId.Value)
                    .Select(x => ((x.FirstName ?? "") + " " + (x.LastName ?? "")).Trim())
                    .FirstOrDefaultAsync(ct) ?? "System";
            }

            context.UserManagementAuditLogs.Add(UserManagementAuditEntity.Create(
                action,
                targetUserId,
                targetAuthId,
                targetDisplayName,
                tenantContext.UserId,
                actorName,
                details,
                succeeded));
            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not persist user management audit action {AuditAction}.", action);
        }
    }

    public async Task<IReadOnlyList<UserManagementAuditItem>> GetRecentAsync(int take = 50, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        take = Math.Clamp(take, 1, 200);

        return await context.UserManagementAuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .Select(x => new UserManagementAuditItem(
                x.Id,
                x.Action,
                x.TargetUserId,
                x.TargetDisplayName,
                x.ActorDisplayName,
                x.Details,
                x.Succeeded,
                x.CreatedAtUtc))
            .ToListAsync(ct);
    }
}
