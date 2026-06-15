using ProjectManagement.Shared.DTO.UserSettings;

namespace Application.Feature.General.ListSettings
{
    /// <summary>
    /// Reads and writes personal list UI-settings (visible columns, column widths,
    /// saved filters, saved column views) for a single user within the current tenant.
    /// </summary>
    public interface IUserListSettingService
    {
        Task<List<UserListSettingDTO>> GetByScopeAsync(int userId, string scope, CancellationToken ct = default);
        Task UpsertAsync(int userId, string scope, string kind, string payload, CancellationToken ct = default);
    }
}
