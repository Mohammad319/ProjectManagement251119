using Application.Feature.Expression;
using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Calculation.Queries;

public sealed record GetShareCalculationPageQuery(int Id, int UserId, int? DepartmentId)
    : IRequest<CalculationPageOtherDepartmentDTO>;

public sealed class GetShareCalculationPageQueryHandler(IShardingSingleDbContext _context)
    : IRequestHandler<GetShareCalculationPageQuery, CalculationPageOtherDepartmentDTO>
{
    public async Task<CalculationPageOtherDepartmentDTO> Handle(GetShareCalculationPageQuery request, CancellationToken cancellationToken)
    {
        return await _context.ShareCalc
            .AsNoTracking()
            .Where(x =>
                x.CalculationId == request.Id &&
                (request.DepartmentId == null || x.DepartmentId == request.DepartmentId) &&
                (x.CreatedAt == null || (x.CreatedBy == request.UserId)))
            .Select(CalculationExpression.SelectCalculationPageOtherDepartmentDTO)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
