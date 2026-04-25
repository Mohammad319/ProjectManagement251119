using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Task.Queries;

public sealed record GetTaskResourceSuggestionsQuery(int TaskId, int MaxResults = 5)
    : IRequest<IReadOnlyList<TaskResourceSuggestionDTO>>;

public sealed class GetTaskResourceSuggestionsQueryHandler(ITaskResourceSuggestionService service)
    : IRequestHandler<GetTaskResourceSuggestionsQuery, IReadOnlyList<TaskResourceSuggestionDTO>>
{
    public Task<IReadOnlyList<TaskResourceSuggestionDTO>> Handle(
        GetTaskResourceSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        return service.GetSuggestionsAsync(request.TaskId, request.MaxResults, cancellationToken);
    }
}
