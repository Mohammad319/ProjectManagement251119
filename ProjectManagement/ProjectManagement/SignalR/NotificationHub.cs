using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Services;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Hub;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.SignalR
{
    internal static class NotificationGroupNames
    {
        public static string Normalize(int? tenantId, string? group)
        {
            if (string.IsNullOrWhiteSpace(group))
                return string.Empty;

            if (tenantId is > 0 && int.TryParse(group, out var calculationId) && calculationId > 0)
                return ForCalculation(tenantId.Value, calculationId);

            return group;
        }

        public static string ForCalculation(int tenantId, int calculationId)
            => $"tenant:{tenantId}:calc:{calculationId}";
    }

    public class SendHubNotification(IHubContext<NotificationHub> hubContext, ITenantContext tenantContext) : INotificationHub
    {
        public async Task SendNotificationAsync(string group, ObjectTypHub type, OperationType operationType, object data)
        {
            var normalizedGroup = NotificationGroupNames.Normalize(tenantContext.TenantId, group);
            if (string.IsNullOrWhiteSpace(normalizedGroup))
                return;

            await hubContext.Clients.Group(normalizedGroup).SendAsync("calc", type, operationType, data);
        }

        public async Task SendNotificationAsync(string group, ObjectTypHub type, OperationType operationType, int parentId, object data)
        {
            var normalizedGroup = NotificationGroupNames.Normalize(tenantContext.TenantId, group);
            if (string.IsNullOrWhiteSpace(normalizedGroup))
                return;

            await hubContext.Clients.Group(normalizedGroup).SendAsync("calc", type, operationType, new HubDataDto { Data = data, ParentId = parentId });
        }
    }

    [Authorize]
    public class NotificationHub(ITenantContext currentTenant, IDbContextFactoryTenant dbFactory) : Hub
    {
        public override Task OnConnectedAsync()
        {
            var isAuth = Context.User?.Identity?.IsAuthenticated == true;
            var tenant = GetTenantId();
            Console.WriteLine($"[Hub] Connected. IsAuth={isAuth}, TenantClaim={tenant?.ToString() ?? "null"}");
            return base.OnConnectedAsync();
        }

        [HubMethodName("AddToGroup")]
        public async Task AddToGroup(int id, CancellationToken ct = default)
        {
            if (id <= 0)
                return;

            var tenantId = GetTenantId();
            if (tenantId is null or <= 0)
                return;

            if (currentTenant is TenantContext mutableTenant && mutableTenant.TenantId <= 0)
                mutableTenant.TenantId = tenantId.Value;

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var calculationExists = await db.Calculations
                .AsNoTracking()
                .AnyAsync(x => x.Id == id, ct);

            if (!calculationExists)
                return;

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                NotificationGroupNames.ForCalculation(tenantId.Value, id),
                ct);
        }

        private int? GetTenantId()
        {
            if (currentTenant.TenantId > 0)
                return currentTenant.TenantId;

            var claim = Context.User?.FindFirst(PMClaimsConst.Tenant)?.Value;
            return int.TryParse(claim, out var tenantId) && tenantId > 0
                ? tenantId
                : null;
        }
    }
}
