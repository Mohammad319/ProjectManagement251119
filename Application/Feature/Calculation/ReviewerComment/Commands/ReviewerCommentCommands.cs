using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.ReviewerComment.Commands
{
    public sealed record SaveReviewerCommentCommand(
        ReviewerCommentSaveDTO Dto,
        int UserId,
        int? DepartmentId,
        bool IsViewer) : IRequest<bool>;

    public class SaveReviewerCommentCommandHandler(IReviewerCommentService service)
        : IRequestHandler<SaveReviewerCommentCommand, bool>
    {
        public Task<bool> Handle(SaveReviewerCommentCommand request, CancellationToken ct)
            => service.SaveAsync(request.Dto, request.UserId, request.DepartmentId, request.IsViewer, ct);
    }
}
