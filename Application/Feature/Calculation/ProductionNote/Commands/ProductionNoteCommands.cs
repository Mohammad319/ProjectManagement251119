using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.ProductionNote.Commands
{
    public sealed record SaveProductionNoteCommand(
        ProductionNoteSaveDTO Dto,
        int UserId,
        int? DepartmentId,
        bool IsViewer) : IRequest<bool>;

    public class SaveProductionNoteCommandHandler(IProductionNoteService service)
        : IRequestHandler<SaveProductionNoteCommand, bool>
    {
        public Task<bool> Handle(SaveProductionNoteCommand request, CancellationToken ct)
            => service.SaveAsync(request.Dto, request.UserId, request.DepartmentId, request.IsViewer, ct);
    }
}
