using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Calculation.TaskStatus.Queries
{
    public sealed record GetTaskStatusQuery() : IRequest<List<TaskStatusEntity>>;

    public sealed class GetTaskStatusQueryHandler(ILookupStatusCommandService<TaskStatusEntity> service)
        : IRequestHandler<GetTaskStatusQuery, List<TaskStatusEntity>>
    {
        public Task<List<TaskStatusEntity>> Handle(GetTaskStatusQuery request, CancellationToken cancellationToken)
            => service.GetAllAsync(cancellationToken);
    }

    public sealed record GetTaskStatusAdminListQuery() : IRequest<IReadOnlyList<LookupAdminListItemDto>>;

    public sealed class GetTaskStatusAdminListQueryHandler(ILookupStatusCommandService<TaskStatusEntity> service)
        : IRequestHandler<GetTaskStatusAdminListQuery, IReadOnlyList<LookupAdminListItemDto>>
    {
        public Task<IReadOnlyList<LookupAdminListItemDto>> Handle(GetTaskStatusAdminListQuery request, CancellationToken cancellationToken)
            => service.GetAllListAsync(cancellationToken);
    }

    public sealed record GetVisualTaskStatusQuery(int? Id) : IRequest<IEnumerable<ListOrderDTO>>;

    public sealed class GetVisualTaskStatusQueryHandler(ILookupStatusCommandService<TaskStatusEntity> service)
        : IRequestHandler<GetVisualTaskStatusQuery, IEnumerable<ListOrderDTO>>
    {
        public Task<IEnumerable<ListOrderDTO>> Handle(GetVisualTaskStatusQuery request, CancellationToken cancellationToken)
            => service.GetVisualAsync(request.Id, cancellationToken);
    }
}
