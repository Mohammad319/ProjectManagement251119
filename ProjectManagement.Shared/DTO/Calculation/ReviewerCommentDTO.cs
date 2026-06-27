using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Shared.DTO.Calculation
{
    /// <summary>
    /// Save/update the reviewer comment (Granskarkommentar) for a calculation row (task or resource).
    /// The reviewer comment is independent of the calculation economy and may be written by an
    /// authorized reviewer (incl. a Viewer with access) even when the calculation is locked.
    /// </summary>
    public sealed class ReviewerCommentSaveDTO
    {
        public CalculationItemType ItemType { get; set; }
        public int ItemId { get; set; }
        public string? Text { get; set; }
    }
}
