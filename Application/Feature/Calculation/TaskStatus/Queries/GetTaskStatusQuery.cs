using Application.Interfaces;
using Domain.Entities.Calculation;

namespace Application.Feature.Calculation.TaskStatus.Queries;

public sealed record GetTaskStatusQuery() : IRequest<List<TaskStatusEntity>>;

public sealed class GetTaskStatusQueryHandler(IShardingSingleDbContext _context)
    : IRequestHandler<GetTaskStatusQuery, List<TaskStatusEntity>>
{
    public async Task<List<TaskStatusEntity>> Handle(GetTaskStatusQuery request, CancellationToken cancellationToken)
    {
        return await _context.TaskStatus.ToListAsync(cancellationToken);
    }
}
