
namespace Application.Feature.Project.Status.Queries
{
    using Domain.Entities.Project;
    using global::Application.Feature.General;
    using global::Application.Interfaces;
    using ProjectManagement.Shared.DTO.General;

    namespace Application.Feature.Calculation.Status.Queries
    {
        public sealed record GetStatusQuery()
            : IRequest<List<StatusEntity>>;

        public sealed class GetStatusQueryHandler
            : IRequestHandler<GetStatusQuery, List<StatusEntity>>
        {
            private readonly ILookupStatusCommandService<StatusEntity> _service;

            public GetStatusQueryHandler(ILookupStatusCommandService<StatusEntity> service)
            {
                _service = service;
            }

            public Task<List<StatusEntity>> Handle(GetStatusQuery request, CancellationToken cancellationToken)
                => _service.GetAllAsync(cancellationToken);
        }

        public sealed record GetVisualStatusQuery(int? Id)
            : IRequest<IEnumerable<ListOrderDTO>>;

        public sealed class GetVisualStatusQueryHandler
            : IRequestHandler<GetVisualStatusQuery, IEnumerable<ListOrderDTO>>
        {
            private readonly ILookupStatusCommandService<StatusEntity> _service;

            public GetVisualStatusQueryHandler(ILookupStatusCommandService<StatusEntity> service)
            {
                _service = service;
            }

            public Task<IEnumerable<ListOrderDTO>> Handle(GetVisualStatusQuery request, CancellationToken cancellationToken)
                => _service.GetVisualAsync(request.Id, cancellationToken);
        }
    }

}
