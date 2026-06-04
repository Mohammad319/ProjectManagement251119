using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.ProjectBid
{
    public interface IProjectBidService
    {
        Task<List<ProjectBidListDTO>> GetByProjectAsync(Guid projectId, int? departmentId, CancellationToken ct = default);
        Task<int> CreateAsync(Guid projectId, ProjectBidPostDTO dto, int? departmentId, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, Guid projectId, ProjectBidPostDTO dto, int? departmentId, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, Guid projectId, int? departmentId, CancellationToken ct = default);
    }
}
