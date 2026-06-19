using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Shared.Repositories.Project.Implement
{
    public sealed class ProjectShareRepository(HTTPRepository httpRepository) : IProjectShareRepository
    {
        private static string Base => PMAPIConst.ProjectShares;

        public Task<List<ProjectShareListItemDTO>> GetByProjectAsync(Guid projectId)
            => httpRepository.GetAsync<List<ProjectShareListItemDTO>>(Base + projectId);

        public Task<int> UpsertAsync(Guid projectId, ProjectShareUpsertDTO dto)
            => httpRepository.PostAsync<int, ProjectShareUpsertDTO>(dto, Base + projectId);

        public Task<bool> DeleteAsync(int id)
            => httpRepository.DeleteAsync<bool>(Base + id);
    }
}
