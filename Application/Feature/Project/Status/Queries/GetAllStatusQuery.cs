using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.Status.Queries
{
    public sealed record GetAllStatusQuery() : IRequest<List<StatusEntity>>;
    public class GetAllStatusQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetAllStatusQuery, List<StatusEntity>>
    {
        public async Task<List<StatusEntity>> Handle(GetAllStatusQuery query, CancellationToken cancellationToken)
        {
            return await context.CalculationStatus.ToListAsync(cancellationToken);
        }
    }
}
