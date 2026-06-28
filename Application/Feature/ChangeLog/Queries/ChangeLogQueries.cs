using Application.Interfaces;
using ProjectManagement.Shared.DTO.ChangeLog;

namespace Application.Feature.ChangeLog.Queries
{
    public sealed record GetRecentProjectChangesQuery(IReadOnlyList<Guid> ProjectIds, int Take = 5)
        : IRequest<IReadOnlyDictionary<Guid, List<ChangeLogItemDTO>>>;

    public sealed class GetRecentProjectChangesQueryHandler(IChangeLogService service)
        : IRequestHandler<GetRecentProjectChangesQuery, IReadOnlyDictionary<Guid, List<ChangeLogItemDTO>>>
    {
        public Task<IReadOnlyDictionary<Guid, List<ChangeLogItemDTO>>> Handle(GetRecentProjectChangesQuery request, CancellationToken ct)
            => service.GetRecentForProjectsAsync(request.ProjectIds ?? [], request.Take, ct);
    }

    public sealed record GetRecentCalculationChangesQuery(IReadOnlyList<int> CalculationIds, int Take = 5)
        : IRequest<IReadOnlyDictionary<int, List<ChangeLogItemDTO>>>;

    public sealed class GetRecentCalculationChangesQueryHandler(IChangeLogService service)
        : IRequestHandler<GetRecentCalculationChangesQuery, IReadOnlyDictionary<int, List<ChangeLogItemDTO>>>
    {
        public Task<IReadOnlyDictionary<int, List<ChangeLogItemDTO>>> Handle(GetRecentCalculationChangesQuery request, CancellationToken ct)
            => service.GetRecentForCalculationsAsync(request.CalculationIds ?? [], request.Take, ct);
    }
}
