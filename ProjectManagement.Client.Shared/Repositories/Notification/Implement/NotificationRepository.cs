using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared.DTO.Notification;

namespace ProjectManagement.Client.Shared.Repositories.Notification.Implement
{
    public sealed class NotificationRepository(HTTPRepository httpRepository) : INotificationRepository
    {
        private static string Base => PMAPIConst.Notifications;

        public Task<NotificationSummaryDTO> GetSummaryAsync(int take = 15)
            => httpRepository.GetAsync<NotificationSummaryDTO>($"{Base}summary?take={take}");

        public Task<List<NotificationListItemDTO>> GetListAsync(int skip = 0, int take = 30)
            => httpRepository.GetAsync<List<NotificationListItemDTO>>($"{Base}?skip={skip}&take={take}");

        public Task<int> GetUnreadCountAsync()
            => httpRepository.GetAsync<int>($"{Base}unread-count");

        public Task<bool> MarkReadAsync(int id)
            => httpRepository.PostAsync<bool, object>(new { }, $"{Base}{id}/read");

        public Task<int> MarkAllReadAsync()
            => httpRepository.PostAsync<int, object>(new { }, $"{Base}read-all");

        public Task<bool> DeleteAsync(int id)
            => httpRepository.DeleteAsync<bool>($"{Base}{id}");

        public Task<int> ClearReadAsync()
            => httpRepository.PostAsync<int, object>(new { }, $"{Base}clear-read");

        public Task<ProjectOpenInfoDTO> GetOpenInfoAsync(Guid projectId)
            => httpRepository.GetAsync<ProjectOpenInfoDTO>($"{Base}open-info/{projectId}");
    }
}
