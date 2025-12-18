using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.Contract.Queries
{
    public sealed record GetContractQuery() : IRequest<List<ContractEntity>>;
    public sealed class GetContractQueryHandler(ILookupStatusCommandService<ContractEntity> service)
                : IRequestHandler<GetContractQuery, List<ContractEntity>>
    {
        public Task<List<ContractEntity>> Handle(GetContractQuery request, CancellationToken cancellationToken)
            => service.GetAllAsync(cancellationToken);
    }
    public sealed record GetVisualContractQuery(int? Id) : IRequest<IEnumerable<ListOrderDTO>>;
    public sealed class GetVisualContractQueryHandler(ILookupStatusCommandService<ContractEntity> service)
                : IRequestHandler<GetVisualContractQuery, IEnumerable<ListOrderDTO>>
    {
        public Task<IEnumerable<ListOrderDTO>> Handle(GetVisualContractQuery request, CancellationToken cancellationToken)
            => service.GetVisualAsync(request.Id, cancellationToken);
    }
}
