using ProjectManagement.Shared.DTO.ChangeLog;
using ProjectManagement.Shared.Enums;

namespace Application.Feature.ChangeLog
{
    /// <summary>
    /// Records and reads coarse change-log entries for projects/calculations. Writes are best-effort
    /// and must never break the originating operation; reads back the latest N per object for the
    /// "Senaste ändringar" indicator tooltip.
    /// </summary>
    public interface IChangeLogService
    {
        Task AppendProjectAsync(Guid projectId, ChangeAction action, int actorUserId, CancellationToken ct = default);

        Task AppendCalculationAsync(int calculationId, ChangeAction action, int actorUserId, CancellationToken ct = default);

        Task<IReadOnlyDictionary<Guid, List<ChangeLogItemDTO>>> GetRecentForProjectsAsync(
            IEnumerable<Guid> projectIds, int take, CancellationToken ct = default);

        Task<IReadOnlyDictionary<int, List<ChangeLogItemDTO>>> GetRecentForCalculationsAsync(
            IEnumerable<int> calculationIds, int take, CancellationToken ct = default);
    }
}
