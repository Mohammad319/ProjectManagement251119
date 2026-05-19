namespace ProjectManagement.Shared.DTO.Calculation;

public enum TaskResourceSuggestionSource
{
    TenantTask = 1,
    BlueprintTask = 2
}

public enum TaskResourceSuggestionFeedbackKind
{
    Accepted = 1,
    Rejected = 2,
    WrongUnit = 3,
    MissingResources = 4,
    WrongResourceType = 5
}

public enum TaskResourceSuggestionFeedbackReviewStatus
{
    Pending = 1,
    Approved = 2,
    Ignored = 3
}

public sealed class TaskResourceSuggestionDTO
{
    public int SourceTaskId { get; set; }
    public string SourceTaskName { get; set; } = string.Empty;
    public decimal? SourceTaskQuantity { get; set; }
    public string SourceTaskUnit { get; set; } = string.Empty;
    public TaskResourceSuggestionSource Source { get; set; }
    public double Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public TaskResourceSuggestionFeedbackKind? Feedback { get; set; }
    public List<ResourcePostDTO> Resources { get; set; } = [];
}

public sealed class TaskResourceSuggestionFeedbackDTO
{
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
}
