using Application.Feature.Calculation.Task;
using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;
namespace Application.Feature.Calculation.Group.Queries;

public sealed record GetTasksByFilterQuery(FilterCalculationItemsDto Filter) : IRequest<List<TaskListDTO>>;

public sealed class GetTasksByFilterQueryHandler(ITaskQueryService service)
        : IRequestHandler<GetTasksByFilterQuery, List<TaskListDTO>>
{
    public Task<List<TaskListDTO>> Handle(GetTasksByFilterQuery request, CancellationToken cancellationToken)
    {
        return service.GetByFilterAsync(request.Filter, cancellationToken);
    }
}
