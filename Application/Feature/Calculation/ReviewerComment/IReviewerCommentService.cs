using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.ReviewerComment
{
    /// <summary>
    /// Handles per-row reviewer comments (Granskarkommentar). Separate from economic calculation data:
    /// does not change quantity/price/cost/calculation and is allowed even on a locked calculation.
    /// The backend enforces access (a Viewer/reviewer may only comment on calculations they can see;
    /// economic fields are never touched here).
    /// </summary>
    public interface IReviewerCommentService
    {
        Task<bool> SaveAsync(ReviewerCommentSaveDTO dto, int userId, int? departmentId, bool isViewer, CancellationToken ct = default);
    }
}
