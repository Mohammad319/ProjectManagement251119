
using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Calculation.TaskStatus.Queries
{
    public sealed record GetTaskStatusQuery()
        : IRequest<List<TaskStatusEntity>>;

    public sealed class GetTaskStatusQueryHandler
        : IRequestHandler<GetTaskStatusQuery, List<TaskStatusEntity>>
    {
        private readonly ILookupStatusCommandService<TaskStatusEntity> _service;

        public GetTaskStatusQueryHandler(ILookupStatusCommandService<TaskStatusEntity> service)
        {
            _service = service;
        }

        public Task<List<TaskStatusEntity>> Handle(GetTaskStatusQuery request, CancellationToken cancellationToken)
            => _service.GetAllAsync(cancellationToken);
    }

    public sealed record GetVisualTaskStatusQuery(int? Id)
        : IRequest<IEnumerable<ListOrderDTO>>;

    public sealed class GetVisualTaskStatusQueryHandler
        : IRequestHandler<GetVisualTaskStatusQuery, IEnumerable<ListOrderDTO>>
    {
        private readonly ILookupStatusCommandService<TaskStatusEntity> _service;

        public GetVisualTaskStatusQueryHandler(ILookupStatusCommandService<TaskStatusEntity> service)
        {
            _service = service;
        }

        public Task<IEnumerable<ListOrderDTO>> Handle(GetVisualTaskStatusQuery request, CancellationToken cancellationToken)
            => _service.GetVisualAsync(request.Id, cancellationToken);
    }
}
