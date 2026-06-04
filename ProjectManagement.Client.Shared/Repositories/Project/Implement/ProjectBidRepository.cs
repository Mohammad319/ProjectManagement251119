using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Shared.Repositories.Project.Implement
{
    public sealed class ProjectBidRepository(HTTPRepository httpRepository) : IProjectBidRepository
    {
        private static string Base => PMAPIConst.ProjectBids;

        public Task<List<ProjectBidListDTO>> GetAllAsync(Guid projectId)
            => httpRepository.GetAsync<List<ProjectBidListDTO>>(Base + projectId);

        public Task<int> CreateAsync(Guid projectId, ProjectBidPostDTO dto)
            => httpRepository.PostAsync<int, ProjectBidPostDTO>(dto, Base + projectId);

        public Task<bool> UpdateAsync(int id, Guid projectId, ProjectBidPostDTO dto)
            => httpRepository.PutAsync(dto, Base + $"{id}/{projectId}");

        public Task<bool> DeleteAsync(int id, Guid projectId)
            => httpRepository.DeleteAsync<bool>(Base + $"{id}/{projectId}");
    }
}
