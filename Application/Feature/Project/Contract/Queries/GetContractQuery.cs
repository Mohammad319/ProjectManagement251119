using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.Contract.Queries
{
    public sealed record GetContractQuery() : IRequest<List<ContractEntity>>;
    public class GetContractQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetContractQuery, List<ContractEntity>>
    {
        public async Task<List<ContractEntity>> Handle(GetContractQuery query, CancellationToken cancellationToken)
        {
            return await context.Contracts.ToListAsync(cancellationToken);
        }
    }
}
