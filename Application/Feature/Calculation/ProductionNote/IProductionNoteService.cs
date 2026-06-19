using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.ProductionNote
{
    /// <summary>
    /// Handles per-row production notes. Separate from economic calculation data:
    /// does not change quantity/price/cost/calculation and is allowed even on a locked calculation.
    /// The backend enforces access (Viewer only sees shared/selected calculations, never private ones).
    /// </summary>
    public interface IProductionNoteService
    {
        Task<bool> SaveAsync(ProductionNoteSaveDTO dto, int userId, int? departmentId, bool isViewer, CancellationToken ct = default);
    }
}
