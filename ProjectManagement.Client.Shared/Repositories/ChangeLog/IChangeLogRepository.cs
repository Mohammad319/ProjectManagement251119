using ProjectManagement.Shared.DTO.ChangeLog;

namespace ProjectManagement.Client.Shared.Repositories.ChangeLog
{
    /// <summary>Fetches the latest change-log entries for the project/calculation change indicator tooltip.</summary>
    public interface IChangeLogRepository
    {
        Task<Dictionary<Guid, List<ChangeLogItemDTO>>> GetRecentForProjectsAsync(IReadOnlyList<Guid> projectIds, int take = 5);

        Task<Dictionary<int, List<ChangeLogItemDTO>>> GetRecentForCalculationsAsync(IReadOnlyList<int> calculationIds, int take = 5);
    }
}
