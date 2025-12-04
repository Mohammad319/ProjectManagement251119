using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.Type.Queries
{
    public sealed record GetProjectTypeQuery() : IRequest<List<TypeEntity>>;
    public class GetProjectTypeQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetProjectTypeQuery, List<TypeEntity>>
    {
        public async Task<List<TypeEntity>> Handle(GetProjectTypeQuery query, CancellationToken cancellationToken)
        {
            return await context.CalcProjectType.ToListAsync(cancellationToken);
        }
    }
}
