using ProjectManagement.Shared.DTO.Calculation;

namespace TaskResourceBlueprints.Entities.Tasks;

public class TaskResourceSuggestionFeedback
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int TargetTaskId { get; set; }
    public string TargetTaskName { get; set; } = string.Empty;
    public string? TargetTaskCode { get; set; }
    public string? TargetTaskUnit { get; set; }
    public decimal? TargetTaskQuantity { get; set; }
    public int SourceTaskId { get; set; }
    public string SourceTaskName { get; set; } = string.Empty;
    public string? SourceTaskUnit { get; set; }
    public decimal? SourceTaskQuantity { get; set; }
    public TaskResourceSuggestionSource Source { get; set; }
    public double Score { get; set; }
    public TaskResourceSuggestionFeedbackKind Feedback { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
