namespace ProjectManagement.Shared.DTO.Calculation;

public enum TaskResourceSuggestionSource
{
    TenantTask = 1,
    BlueprintTask = 2
}

public sealed class TaskResourceSuggestionDTO
{
    public int SourceTaskId { get; set; }
    public string SourceTaskName { get; set; } = string.Empty;
    public TaskResourceSuggestionSource Source { get; set; }
    public double Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<ResourcePostDTO> Resources { get; set; } = [];
}
