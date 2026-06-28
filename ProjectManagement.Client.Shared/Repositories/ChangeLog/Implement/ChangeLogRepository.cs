using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared.DTO.ChangeLog;

namespace ProjectManagement.Client.Shared.Repositories.ChangeLog.Implement
{
    public sealed class ChangeLogRepository(HTTPRepository httpRepository) : IChangeLogRepository
    {
        private static string Base => PMAPIConst.ChangeLog;

        public async Task<Dictionary<Guid, List<ChangeLogItemDTO>>> GetRecentForProjectsAsync(IReadOnlyList<Guid> projectIds, int take = 5)
        {
            if (projectIds is null || projectIds.Count == 0)
                return new Dictionary<Guid, List<ChangeLogItemDTO>>();

            return await httpRepository.PostAsync<Dictionary<Guid, List<ChangeLogItemDTO>>, IReadOnlyList<Guid>>(
                projectIds, $"{Base}projects?take={take}")
                ?? new Dictionary<Guid, List<ChangeLogItemDTO>>();
        }

        public async Task<Dictionary<int, List<ChangeLogItemDTO>>> GetRecentForCalculationsAsync(IReadOnlyList<int> calculationIds, int take = 5)
        {
            if (calculationIds is null || calculationIds.Count == 0)
                return new Dictionary<int, List<ChangeLogItemDTO>>();

            return await httpRepository.PostAsync<Dictionary<int, List<ChangeLogItemDTO>>, IReadOnlyList<int>>(
                calculationIds, $"{Base}calculations?take={take}")
                ?? new Dictionary<int, List<ChangeLogItemDTO>>();
        }
    }
}
