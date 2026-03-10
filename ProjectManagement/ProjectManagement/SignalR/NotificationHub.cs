using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
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
    public class NotificationHub(ITenantContext currentTenant) : Hub
    {
        public override Task OnConnectedAsync()
        {
            var isAuth = Context.User?.Identity?.IsAuthenticated == true;
            var tenant = Context.User?.FindFirst(PMClaimsConst.Tenant)?.Value;
            Console.WriteLine($"[Hub] Connected. IsAuth={isAuth}, TenantClaim={tenant ?? "null"}");
            return base.OnConnectedAsync();
        }

        [HubMethodName("AddToGroup")]
        public async Task AddToGroup(int id, CancellationToken ct = default)
        {
            var tenantId = currentTenant.TenantId;
            if (tenantId <= 0)
            {
                var tenantClaim = Context.User?.FindFirst(PMClaimsConst.Tenant)?.Value
                                 ?? Context.User?.FindFirst("TenantID")?.Value;

                if (int.TryParse(tenantClaim, out var parsedTenantId) && parsedTenantId > 0)
                    tenantId = parsedTenantId;
            }

            if (id <= 0 || tenantId <= 0)
                return;

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                NotificationGroupNames.ForCalculation(tenantId, id),
                ct);
        }
    }
}
