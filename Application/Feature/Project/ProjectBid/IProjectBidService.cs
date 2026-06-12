using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.ProjectBid
{
    public interface IProjectBidService
    {
        Task<ProjectBidsViewDTO> GetViewAsync(Guid projectId, int? departmentId, CancellationToken ct = default);
        Task<int> CreateAsync(Guid projectId, ProjectBidPostDTO dto, int? departmentId, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, Guid projectId, ProjectBidPostDTO dto, int? departmentId, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, Guid projectId, int? departmentId, CancellationToken ct = default);

        Task<int> CreatePriceColumnAsync(Guid projectId, ProjectBidPriceColumnPostDTO dto, int? departmentId, CancellationToken ct = default);
        Task<bool> RenamePriceColumnAsync(int id, Guid projectId, ProjectBidPriceColumnPostDTO dto, int? departmentId, CancellationToken ct = default);
        Task<bool> DeletePriceColumnAsync(int id, Guid projectId, int? departmentId, CancellationToken ct = default);
        Task<bool> MovePriceColumnAsync(int id, Guid projectId, int direction, int? departmentId, CancellationToken ct = default);
    }
}
