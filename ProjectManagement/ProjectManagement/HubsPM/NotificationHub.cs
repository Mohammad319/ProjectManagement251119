using Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using ProjectManagement.Shared.DTO.Hub;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Server.HubsPM
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
    public class NotificationHub : Hub
    {
        [HubMethodName("AddToGroup")]
        public async Task AddToGroup(int id)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, id.ToString());
        }
    }
}
