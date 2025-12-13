using Application.Interfaces;
using Application.Services.CalculationItems.CalcShare;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.CalcShare.Queries
{
    public sealed record GetShareQuery(int Id, int? DepartmentId, int UserId) : IRequest<IEnumerable<ListShareCalcDTO>>;

    public sealed class GetShareQueryHandler(IShareCalcService service)
        : IRequestHandler<GetShareQuery, IEnumerable<ListShareCalcDTO>>
    {
        public Task<IEnumerable<ListShareCalcDTO>> Handle(GetShareQuery request, CancellationToken ct)
            => service.GetAsync(request.Id, request.DepartmentId, request.UserId, ct);
    }
}
