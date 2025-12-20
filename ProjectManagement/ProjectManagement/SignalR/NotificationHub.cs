using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Hub;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.SignalR
{
    public class SendHubNotification(IHubContext<NotificationHub> HubContext) : INotificationHub
    {
        public async Task SendNotificationAsync(string group, ObjectTypHub type, OperationType operationType, object data)
        {
            await HubContext.Clients.Group(group).SendAsync("calc", type, operationType, data);
        }

        public async Task SendNotificationAsync(string group, ObjectTypHub type, OperationType operationType, int parentId, object data)
        {
            await HubContext.Clients.Group(group).SendAsync("calc", type, operationType, new HubDataDto() { Data = data, ParentId = parentId });
        }
    }

    [Authorize]
    public class NotificationHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            var isAuth = Context.User?.Identity?.IsAuthenticated == true;
            var tenant = Context.User?.FindFirst(PMClaimsConst.Tentan)?.Value;
            Console.WriteLine($"[Hub] Connected. IsAuth={isAuth}, TenantClaim={tenant ?? "null"}");
            return base.OnConnectedAsync();
        }

        [HubMethodName("AddToGroup")]
        public async Task AddToGroup(int id)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, id.ToString());
        }
    }
}
