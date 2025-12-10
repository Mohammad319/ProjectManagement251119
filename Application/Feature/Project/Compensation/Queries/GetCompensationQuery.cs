using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.Compensation.Queries
{
    public sealed record GetCompensationQuery() : IRequest<List<CompensationEntity>>;
    public class GetCompensationQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetCompensationQuery, List<CompensationEntity>>
    {
        public async Task<List<CompensationEntity>> Handle(GetCompensationQuery query, CancellationToken cancellationToken)
        {
            return await context.Compensations.ToListAsync(cancellationToken);
        }
    }
}
