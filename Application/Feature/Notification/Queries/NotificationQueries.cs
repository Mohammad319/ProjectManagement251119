using Application.Interfaces;
using ProjectManagement.Shared.DTO.Notification;

namespace Application.Feature.Notification.Queries
{
    public sealed record GetNotificationSummaryQuery(int UserId, int Take = 15)
        : IRequest<NotificationSummaryDTO>;

    public sealed class GetNotificationSummaryQueryHandler(INotificationService service)
        : IRequestHandler<GetNotificationSummaryQuery, NotificationSummaryDTO>
    {
        public Task<NotificationSummaryDTO> Handle(GetNotificationSummaryQuery request, CancellationToken ct)
            => service.GetSummaryAsync(request.UserId, request.Take, ct);
    }

    public sealed record GetNotificationsQuery(int UserId, int Skip = 0, int Take = 30)
        : IRequest<IReadOnlyList<NotificationListItemDTO>>;

    public sealed class GetNotificationsQueryHandler(INotificationService service)
        : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationListItemDTO>>
    {
        public Task<IReadOnlyList<NotificationListItemDTO>> Handle(GetNotificationsQuery request, CancellationToken ct)
            => service.GetListAsync(request.UserId, request.Skip, request.Take, ct);
    }

    public sealed record GetUnreadNotificationCountQuery(int UserId) : IRequest<int>;

    public sealed class GetUnreadNotificationCountQueryHandler(INotificationService service)
        : IRequestHandler<GetUnreadNotificationCountQuery, int>
    {
        public Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken ct)
            => service.GetUnreadCountAsync(request.UserId, ct);
    }

    public sealed record GetProjectOpenInfoQuery(Guid ProjectId, int UserId, int? DepartmentId, bool IsViewer)
        : IRequest<ProjectOpenInfoDTO>;

    public sealed class GetProjectOpenInfoQueryHandler(INotificationService service)
        : IRequestHandler<GetProjectOpenInfoQuery, ProjectOpenInfoDTO>
    {
        public Task<ProjectOpenInfoDTO> Handle(GetProjectOpenInfoQuery request, CancellationToken ct)
            => service.GetProjectOpenInfoAsync(request.ProjectId, request.UserId, request.DepartmentId, request.IsViewer, ct);
    }
}
