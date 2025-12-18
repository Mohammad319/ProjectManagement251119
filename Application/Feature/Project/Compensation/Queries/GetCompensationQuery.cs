
using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.Compensation.Queries
{
    public sealed record GetCompensationQuery()
        : IRequest<List<CompensationEntity>>;

    public sealed class GetCompensationQueryHandler(ILookupStatusCommandService<CompensationEntity> service)
                : IRequestHandler<GetCompensationQuery, List<CompensationEntity>>
    {
        public Task<List<CompensationEntity>> Handle(GetCompensationQuery request, CancellationToken cancellationToken)
            => service.GetAllAsync(cancellationToken);
    }

    public sealed record GetVisualCompensationQuery(int? Id)
        : IRequest<IEnumerable<ListOrderDTO>>;

    public sealed class GetVisualCompensationQueryHandler(ILookupStatusCommandService<CompensationEntity> _service)
                : IRequestHandler<GetVisualCompensationQuery, IEnumerable<ListOrderDTO>>
    {

        public Task<IEnumerable<ListOrderDTO>> Handle(GetVisualCompensationQuery request, CancellationToken cancellationToken)
            => _service.GetVisualAsync(request.Id, cancellationToken);
    }
}
