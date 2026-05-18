using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Task.Queries;

public sealed record GetTaskResourceSuggestionsQuery(int TaskId, int MaxResults = 5, bool IncludeResources = true)
    : IRequest<IReadOnlyList<TaskResourceSuggestionDTO>>;

public sealed record GetTaskResourceSuggestionResourcesQuery(
    int SourceTaskId,
    TaskResourceSuggestionSource Source)
    : IRequest<IReadOnlyList<ResourcePostDTO>>;

public sealed record RecordTaskResourceSuggestionFeedbackCommand(TaskResourceSuggestionFeedbackDTO Feedback)
    : IRequest<bool>;

public sealed class GetTaskResourceSuggestionsQueryHandler(ITaskResourceSuggestionService service)
    : IRequestHandler<GetTaskResourceSuggestionsQuery, IReadOnlyList<TaskResourceSuggestionDTO>>
{
    public Task<IReadOnlyList<TaskResourceSuggestionDTO>> Handle(
        GetTaskResourceSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        return service.GetSuggestionsAsync(
            request.TaskId,
            request.MaxResults,
            request.IncludeResources,
            cancellationToken);
    }
}

public sealed class GetTaskResourceSuggestionResourcesQueryHandler(ITaskResourceSuggestionService service)
    : IRequestHandler<GetTaskResourceSuggestionResourcesQuery, IReadOnlyList<ResourcePostDTO>>
{
    public Task<IReadOnlyList<ResourcePostDTO>> Handle(
        GetTaskResourceSuggestionResourcesQuery request,
        CancellationToken cancellationToken)
    {
        return service.GetSuggestionResourcesAsync(
            request.SourceTaskId,
            request.Source,
            cancellationToken);
    }
}

public sealed class RecordTaskResourceSuggestionFeedbackCommandHandler(ITaskResourceSuggestionService service)
    : IRequestHandler<RecordTaskResourceSuggestionFeedbackCommand, bool>
{
    public Task<bool> Handle(
        RecordTaskResourceSuggestionFeedbackCommand request,
        CancellationToken cancellationToken)
    {
        return service.RecordFeedbackAsync(request.Feedback, cancellationToken);
    }
}
