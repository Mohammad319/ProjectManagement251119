using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.CalcShare.Queries
{
    public sealed record GetShareQuery(int Id, int? DepartmentId, int UserId) : IRequest<IReadOnlyList<ListShareCalcDTO>>;

    public sealed class GetShareQueryHandler(IShareCalcService service)
        : IRequestHandler<GetShareQuery, IReadOnlyList<ListShareCalcDTO>>
    {
        public Task<IReadOnlyList<ListShareCalcDTO>> Handle(GetShareQuery request, CancellationToken ct)
            => service.GetAsync(request.Id, request.DepartmentId, request.UserId, ct);
    }
}
