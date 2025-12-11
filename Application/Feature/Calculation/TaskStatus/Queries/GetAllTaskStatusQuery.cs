using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Calculation.TaskStatus.Queries
{
    // GET ALL
    public sealed record GetAllTaskStatusQuery : IRequest<List<ListOrderDTO>>;

    public sealed class GetAllTaskStatusQueryHandler(ITaskStatusQueryService service)
                : IRequestHandler<GetAllTaskStatusQuery, List<ListOrderDTO>>
    {
        public Task<List<ListOrderDTO>> Handle(GetAllTaskStatusQuery request, CancellationToken ct)
            => service.GetAllAsync(ct);
    }

    // GET BY ID
    public sealed record GetTaskStatusByIdQuery(int Id) : IRequest<ListOrderDTO?>;

    public sealed class GetTaskStatusByIdQueryHandler(ITaskStatusQueryService _service)
                : IRequestHandler<GetTaskStatusByIdQuery, ListOrderDTO?>
    {

        public Task<ListOrderDTO?> Handle(GetTaskStatusByIdQuery request, CancellationToken ct)
            => _service.GetByIdAsync(request.Id, ct);
    }
}
