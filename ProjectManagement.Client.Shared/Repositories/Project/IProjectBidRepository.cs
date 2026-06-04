using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Shared.Repositories.Project
{
    public interface IProjectBidRepository
    {
        Task<List<ProjectBidListDTO>> GetAllAsync(Guid projectId);
        Task<int> CreateAsync(Guid projectId, ProjectBidPostDTO dto);
        Task<bool> UpdateAsync(int id, Guid projectId, ProjectBidPostDTO dto);
        Task<bool> DeleteAsync(int id, Guid projectId);
    }
}
