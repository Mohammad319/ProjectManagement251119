using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.ProcurementMethods.Queries
{
    public sealed record GetProcurementMethodsQuery() : IRequest<List<ProcurementMethodEntity>>;
    public class GetProcurementMethodsQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetProcurementMethodsQuery, List<ProcurementMethodEntity>>
    {
        public async Task<List<ProcurementMethodEntity>> Handle(GetProcurementMethodsQuery query, CancellationToken cancellationToken)
        {
            return await context.ProcurementMethod.ToListAsync(cancellationToken);
        }
    }
}
