using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;

namespace Application.Feature.Project.ProjectBid
{
    public interface IProjectBidService
    {
        Task<ProjectBidsViewDTO> GetViewAsync(Guid projectId, int userId, int? departmentId, bool isViewer, CancellationToken ct = default);
        Task<List<ProjectBidComparisonRowDTO>> GetComparisonAsync(IReadOnlyList<Guid> projectIds, int userId, int? departmentId, bool isViewer, CancellationToken ct = default);
        Task<int> CreateAsync(Guid projectId, ProjectBidPostDTO dto, int userId, int? departmentId, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, Guid projectId, ProjectBidPostDTO dto, int userId, int? departmentId, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, Guid projectId, int userId, int? departmentId, CancellationToken ct = default);
        Task<bool> SetEvaluationAsync(Guid projectId, BidEvaluationBasis basis, BidEvaluationModel method, int userId, int? departmentId, CancellationToken ct = default);

        Task<int> CreatePriceColumnAsync(Guid projectId, ProjectBidPriceColumnPostDTO dto, int userId, int? departmentId, CancellationToken ct = default);
        Task<bool> RenamePriceColumnAsync(int id, Guid projectId, ProjectBidPriceColumnPostDTO dto, int userId, int? departmentId, CancellationToken ct = default);
        Task<bool> DeletePriceColumnAsync(int id, Guid projectId, int userId, int? departmentId, CancellationToken ct = default);
        Task<bool> MovePriceColumnAsync(int id, Guid projectId, int direction, int userId, int? departmentId, CancellationToken ct = default);
    }
}
