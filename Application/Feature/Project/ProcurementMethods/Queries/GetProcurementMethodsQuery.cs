using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.ProcurementMethods.Queries
{
    public sealed record GetProcurementMethodsQuery() : IRequest<List<ProcurementMethodsEntity>>;
    public class GetProcurementMethodsQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetProcurementMethodsQuery, List<ProcurementMethodsEntity>>
    {
        public async Task<List<ProcurementMethodsEntity>> Handle(GetProcurementMethodsQuery query, CancellationToken cancellationToken)
        {
            return await context.ProcurementMethod.ToListAsync(cancellationToken);
        }
    }
}
