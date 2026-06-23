namespace Application.Interfaces
{
    /// <summary>
    /// Pushes a realtime "your notifications changed" signal to specific users so their
    /// notification bell/badge can refresh immediately (without waiting for a page reload).
    /// Implemented over SignalR in the presentation layer.
    /// </summary>
    public interface INotificationPublisher
    {
        /// <summary>
        /// Notifies the given users that their unread notifications changed. Best-effort:
        /// failures must never break the originating operation (e.g. a project share).
        /// </summary>
        Task PublishUnreadChangedAsync(IEnumerable<int> userIds, CancellationToken ct = default);
    }
}
