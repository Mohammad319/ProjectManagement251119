using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Shared.DTO.Calculation
{
    /// <summary>
    /// Save/update the production note for a calculation row (task or resource).
    /// The production note is independent of the calculation economy and may be written
    /// by an authorized user (incl. Viewer) even when the calculation is locked.
    /// </summary>
    public sealed class ProductionNoteSaveDTO
    {
        public CalculationItemType ItemType { get; set; }
        public int ItemId { get; set; }
        public string? Text { get; set; }
    }
}
