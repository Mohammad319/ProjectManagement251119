using ProjectManagement.Shared.DTO.Notification;

namespace ProjectManagement.Client.Shared.Repositories.Notification
{
    public interface INotificationRepository
    {
        Task<NotificationSummaryDTO> GetSummaryAsync(int take = 15);
        Task<List<NotificationListItemDTO>> GetListAsync(int skip = 0, int take = 30);
        Task<int> GetUnreadCountAsync();
        Task<bool> MarkReadAsync(int id);
        Task<int> MarkAllReadAsync();
        Task<bool> DeleteAsync(int id);
        Task<int> ClearReadAsync();
        Task<ProjectOpenInfoDTO> GetOpenInfoAsync(Guid projectId);
    }
}
