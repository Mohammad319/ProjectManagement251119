using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.Type.Queries
{
    public sealed record GetTypeQuery()
        : IRequest<List<TypeEntity>>;

    public sealed class GetTypeQueryHandler
        : IRequestHandler<GetTypeQuery, List<TypeEntity>>
    {
        private readonly ILookupStatusCommandService<TypeEntity> _service;

        public GetTypeQueryHandler(ILookupStatusCommandService<TypeEntity> service)
        {
            _service = service;
        }

        public Task<List<TypeEntity>> Handle(GetTypeQuery request, CancellationToken cancellationToken)
            => _service.GetAllAsync(cancellationToken);
    }

    // نسخة خفيفة للـ UI (Id, Name, SortOrder)
    public sealed record GetVisualTypeQuery(int? Id)
        : IRequest<IEnumerable<ListOrderDTO>>;

    public sealed class GetVisualTypeQueryHandler
        : IRequestHandler<GetVisualTypeQuery, IEnumerable<ListOrderDTO>>
    {
        private readonly ILookupStatusCommandService<TypeEntity> _service;

        public GetVisualTypeQueryHandler(ILookupStatusCommandService<TypeEntity> service)
        {
            _service = service;
        }

        public Task<IEnumerable<ListOrderDTO>> Handle(GetVisualTypeQuery request, CancellationToken cancellationToken)
            => _service.GetVisualAsync(request.Id, cancellationToken);
    }
}
