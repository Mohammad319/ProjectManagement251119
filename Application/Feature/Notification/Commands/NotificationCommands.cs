using Application.Interfaces;

namespace Application.Feature.Notification.Commands
{
    public sealed record MarkNotificationReadCommand(int Id, int UserId) : IRequest<bool>;

    public sealed class MarkNotificationReadCommandHandler(INotificationService service)
        : IRequestHandler<MarkNotificationReadCommand, bool>
    {
        public Task<bool> Handle(MarkNotificationReadCommand request, CancellationToken ct)
            => service.MarkAsReadAsync(request.Id, request.UserId, ct);
    }

    public sealed record MarkAllNotificationsReadCommand(int UserId) : IRequest<int>;

    public sealed class MarkAllNotificationsReadCommandHandler(INotificationService service)
        : IRequestHandler<MarkAllNotificationsReadCommand, int>
    {
        public Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
            => service.MarkAllAsReadAsync(request.UserId, ct);
    }

    public sealed record DeleteNotificationCommand(int Id, int UserId) : IRequest<bool>;

    public sealed class DeleteNotificationCommandHandler(INotificationService service)
        : IRequestHandler<DeleteNotificationCommand, bool>
    {
        public Task<bool> Handle(DeleteNotificationCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, request.UserId, ct);
    }

    public sealed record ClearReadNotificationsCommand(int UserId) : IRequest<int>;

    public sealed class ClearReadNotificationsCommandHandler(INotificationService service)
        : IRequestHandler<ClearReadNotificationsCommand, int>
    {
        public Task<int> Handle(ClearReadNotificationsCommand request, CancellationToken ct)
            => service.ClearReadAsync(request.UserId, ct);
    }
}
