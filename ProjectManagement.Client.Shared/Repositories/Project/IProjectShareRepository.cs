using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Shared.Repositories.Project
{
    public interface IProjectShareRepository
    {
        Task<List<ProjectShareListItemDTO>> GetByProjectAsync(Guid projectId);
        Task<int> UpsertAsync(Guid projectId, ProjectShareUpsertDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
