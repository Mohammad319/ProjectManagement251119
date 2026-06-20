using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Client.Shared.Repositories.Project.Implement
{
    public sealed class ProjectBidRepository(HTTPRepository httpRepository) : IProjectBidRepository
    {
        private static string Base => PMAPIConst.ProjectBids;

        public Task<ProjectBidsViewDTO> GetViewAsync(Guid projectId)
            => httpRepository.GetAsync<ProjectBidsViewDTO>(Base + projectId);

        public Task<List<ProjectBidComparisonRowDTO>> GetComparisonAsync(IReadOnlyList<Guid> projectIds)
            => httpRepository.PostAsync<List<ProjectBidComparisonRowDTO>, List<Guid>>(projectIds.ToList(), Base + "comparison");

        public Task<int> CreateAsync(Guid projectId, ProjectBidPostDTO dto)
            => httpRepository.PostAsync<int, ProjectBidPostDTO>(dto, Base + projectId);

        public Task<bool> UpdateAsync(int id, Guid projectId, ProjectBidPostDTO dto)
            => httpRepository.PutAsync(dto, Base + $"{id}/{projectId}");

        public Task<bool> DeleteAsync(int id, Guid projectId)
            => httpRepository.DeleteAsync<bool>(Base + $"{id}/{projectId}");

        public Task<bool> SetEvaluationAsync(Guid projectId, BidEvaluationBasis basis, BidEvaluationModel method)
            => httpRepository.PutAsync(new { }, Base + $"{projectId}/evaluation/{(int)basis}/{(int)method}");

        public Task<int> CreateColumnAsync(Guid projectId, ProjectBidPriceColumnPostDTO dto)
            => httpRepository.PostAsync<int, ProjectBidPriceColumnPostDTO>(dto, Base + $"column/{projectId}");

        public Task<bool> RenameColumnAsync(int id, Guid projectId, ProjectBidPriceColumnPostDTO dto)
            => httpRepository.PutAsync(dto, Base + $"column/{id}/{projectId}");

        public Task<bool> DeleteColumnAsync(int id, Guid projectId)
            => httpRepository.DeleteAsync<bool>(Base + $"column/{id}/{projectId}");

        public Task<bool> MoveColumnAsync(int id, Guid projectId, int direction)
            => httpRepository.PutAsync(new { }, Base + $"column/{id}/{projectId}/move/{direction}");
    }
}
