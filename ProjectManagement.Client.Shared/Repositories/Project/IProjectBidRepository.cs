using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Client.Shared.Repositories.Project
{
    public interface IProjectBidRepository
    {
        Task<ProjectBidsViewDTO> GetViewAsync(Guid projectId);
        Task<int> CreateAsync(Guid projectId, ProjectBidPostDTO dto);
        Task<bool> UpdateAsync(int id, Guid projectId, ProjectBidPostDTO dto);
        Task<bool> DeleteAsync(int id, Guid projectId);
        Task<bool> SetEvaluationModelAsync(Guid projectId, BidEvaluationModel model);

        Task<int> CreateColumnAsync(Guid projectId, ProjectBidPriceColumnPostDTO dto);
        Task<bool> RenameColumnAsync(int id, Guid projectId, ProjectBidPriceColumnPostDTO dto);
        Task<bool> DeleteColumnAsync(int id, Guid projectId);
        Task<bool> MoveColumnAsync(int id, Guid projectId, int direction);
    }
}
