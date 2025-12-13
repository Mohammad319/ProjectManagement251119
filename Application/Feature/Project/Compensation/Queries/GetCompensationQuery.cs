
using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.Compensation.Queries
{
    public sealed record GetCompensationQuery()
        : IRequest<List<CompensationEntity>>;

    public sealed class GetCompensationQueryHandler(ILookupStatusQueryService<CompensationEntity> service)
                : IRequestHandler<GetCompensationQuery, List<CompensationEntity>>
    {
        public Task<List<CompensationEntity>> Handle(GetCompensationQuery request, CancellationToken cancellationToken)
            => service.GetAllAsync(cancellationToken);
    }

    public sealed record GetVisualCompensationQuery(int? Id)
        : IRequest<IEnumerable<ListOrderDTO>>;

    public sealed class GetVisualCompensationQueryHandler
        : IRequestHandler<GetVisualCompensationQuery, IEnumerable<ListOrderDTO>>
    {
        private readonly ILookupStatusQueryService<CompensationEntity> _service;

        public GetVisualCompensationQueryHandler(ILookupStatusQueryService<CompensationEntity> service)
        {
            _service = service;
        }

        public Task<IEnumerable<ListOrderDTO>> Handle(GetVisualCompensationQuery request, CancellationToken cancellationToken)
            => _service.GetVisualAsync(request.Id, cancellationToken);
    }
}
