using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Task;

public interface ITaskResourceSuggestionService
{
    Task<IReadOnlyList<TaskResourceSuggestionDTO>> GetSuggestionsAsync(
        int taskId,
        int maxResults,
        CancellationToken cancellationToken = default);
}
