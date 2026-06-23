using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Hub;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.SignalR
{
    public class SendHubNotification(IHubContext<NotificationHub> hubContext) : INotificationHub
    {
        public async Task SendNotificationAsync(string group, ObjectTypHub type, OperationType operationType, object data)
        {
            await hubContext.Clients.Group(group).SendAsync("calc", type, operationType, data);
        }

        public async Task SendNotificationAsync(string group, ObjectTypHub type, OperationType operationType, int parentId, object data)
        {
            await hubContext.Clients.Group(group).SendAsync("calc", type, operationType, new HubDataDto() { Data = data, ParentId = parentId });
        }
    }

    [Authorize]
    public class NotificationHub(ILogger<NotificationHub> logger) : Hub
    {
        /// <summary>SignalR group that receives a connected user's personal notification events.</summary>
        public static string UserGroup(int userId) => $"notif-user-{userId}";

        public override async Task OnConnectedAsync()
        {
            var isAuth = Context.User?.Identity?.IsAuthenticated == true;
            var tenant = Context.User?.FindFirst(PMClaimsConst.Tenant)?.Value;
            logger.LogInformation("[Hub] Connected. IsAuth={IsAuth}, TenantClaim={TenantClaim}", isAuth, tenant ?? "null");

            // Auto-join the user's personal notification channel based on the authenticated claim
            // (never a client-supplied id) so notification pushes reach exactly this user.
            if (int.TryParse(Context.User?.FindFirst(PMClaimsConst.UserId)?.Value, out var userId) && userId > 0)
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

            await base.OnConnectedAsync();
        }

        [HubMethodName("AddToGroup")]
        public async Task AddToGroup(int id)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, id.ToString());
        }

        [HubMethodName("RemoveFromGroup")]
        public async Task RemoveFromGroup(int id)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, id.ToString());
        }
    }
}
