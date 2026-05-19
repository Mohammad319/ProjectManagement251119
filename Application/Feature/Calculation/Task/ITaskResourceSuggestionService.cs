using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Task;

public interface ITaskResourceSuggestionService
{
    Task<IReadOnlyList<TaskResourceSuggestionDTO>> GetSuggestionsAsync(
        int taskId,
        int maxResults,
        bool includeResources = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResourcePostDTO>> GetSuggestionResourcesAsync(
        int sourceTaskId,
        TaskResourceSuggestionSource source,
        CancellationToken cancellationToken = default);

    Task<bool> RecordFeedbackAsync(
        TaskResourceSuggestionFeedbackDTO feedback,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, IReadOnlyList<TaskResourceSuggestionDTO>>> GetBulkSuggestionsAsync(
        IReadOnlyList<int> taskIds,
        int maxResultsPerTask,
        CancellationToken cancellationToken = default);
}
