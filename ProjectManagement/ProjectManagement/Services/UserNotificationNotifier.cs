using System.Collections.Concurrent;
using Application.Interfaces;

namespace ProjectManagement.Services
{
    /// <summary>
    /// Singleton, in-memory implementation of <see cref="IUserNotificationNotifier"/>. Keeps a set of
    /// per-user change handlers (one per open notification bell) and invokes them when a notification
    /// is created/updated for that user. Handlers are invoked fire-and-forget so the originating
    /// operation is never blocked or broken by a slow/failed UI update.
    /// </summary>
    public sealed class UserNotificationNotifier(ILogger<UserNotificationNotifier> logger) : IUserNotificationNotifier
    {
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<Guid, Func<Task>>> _subscribers = new();

        public void Notify(IEnumerable<int> userIds)
        {
            foreach (var userId in userIds.Where(id => id > 0).Distinct())
            {
                if (!_subscribers.TryGetValue(userId, out var handlers))
                    continue;

                foreach (var handler in handlers.Values)
                    _ = InvokeSafeAsync(handler);
            }
        }

        public IDisposable Subscribe(int userId, Func<Task> onChanged)
        {
            ArgumentNullException.ThrowIfNull(onChanged);

            var handlers = _subscribers.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Func<Task>>());
            var key = Guid.NewGuid();
            handlers[key] = onChanged;

            return new Subscription(() =>
            {
                if (_subscribers.TryGetValue(userId, out var set))
                {
                    set.TryRemove(key, out _);
                    if (set.IsEmpty)
                        _subscribers.TryRemove(userId, out _);
                }
            });
        }

        private async Task InvokeSafeAsync(Func<Task> handler)
        {
            try
            {
                await handler();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Notification change handler failed.");
            }
        }

        private sealed class Subscription(Action dispose) : IDisposable
        {
            private Action? _dispose = dispose;

            public void Dispose()
            {
                var d = Interlocked.Exchange(ref _dispose, null);
                d?.Invoke();
            }
        }
    }
}
