using Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ProjectManagement.SignalR
{
    /// <summary>
    /// Implementation of <see cref="INotificationPublisher"/>. Drives two realtime paths:
    /// <list type="bullet">
    /// <item>the in-process <see cref="IUserNotificationNotifier"/> – reliably refreshes the
    /// server-rendered notification bell within this instance;</item>
    /// <item>a SignalR "notif" event to each user's group – for any (future) client-side listeners
    /// and cross-instance fan-out via a configured backplane.</item>
    /// </list>
    /// </summary>
    public sealed class NotificationPublisher(
        IUserNotificationNotifier notifier,
        IHubContext<NotificationHub> hubContext) : INotificationPublisher
    {
        public async Task PublishUnreadChangedAsync(IEnumerable<int> userIds, CancellationToken ct = default)
        {
            var ids = userIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0)
                return;

            notifier.Notify(ids);

            foreach (var userId in ids)
                await hubContext.Clients.Group(NotificationHub.UserGroup(userId)).SendAsync("notif", ct);
        }
    }
}
