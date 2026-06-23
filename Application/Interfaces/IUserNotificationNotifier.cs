namespace Application.Interfaces
{
    /// <summary>
    /// In-process realtime channel for notification changes. Bridges the request scope that creates a
    /// notification (e.g. a project share) and the interactive server circuits that render a user's
    /// notification bell – without requiring an authenticated client SignalR connection from the server.
    /// Single-instance scope; cross-instance fan-out is handled separately (SignalR).
    /// </summary>
    public interface IUserNotificationNotifier
    {
        /// <summary>Signals the given users that their notifications changed (fire-and-forget).</summary>
        void Notify(IEnumerable<int> userIds);

        /// <summary>Subscribes a handler to a user's change signal. Dispose to unsubscribe.</summary>
        IDisposable Subscribe(int userId, Func<Task> onChanged);
    }
}
