using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Calculation.Queries.Attribute;

public sealed record HourlyPriceListQuery(int Id, int? DepartmentId)
    : IRequest<List<HourlyPriceListGroupDTO>>;

public sealed class HourlyPriceListQueryHandler(IShardingSingleDbContext _dataAccess)
    : IRequestHandler<HourlyPriceListQuery, List<HourlyPriceListGroupDTO>>
{
    public async Task<List<HourlyPriceListGroupDTO>> Handle(HourlyPriceListQuery request, CancellationToken cancellationToken)
    {
        var calculation = await _dataAccess.Calculations
            .AsNoTracking()
            .Where(x => x.Id == request.Id &&
                        (!request.DepartmentId.HasValue || x.Project.Folder.DepartmentId == request.DepartmentId))
            .Select(x => x.HourlyPriceFactorData.HourlyPrice)
            .FirstOrDefaultAsync(cancellationToken);

        return calculation ?? [];
    }
}
