using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;
using System;

namespace Application.Feature.Calculation.Calculation.Queries;

public sealed record GetAllCalculationsByDepartmentQuery(Guid ProjectId, int UserId, int? DepartmentId)
    : IRequest<IEnumerable<ListCalculationDTO>>;

public sealed class GetAllCalculationsByDepartmentQueryHandler(IShardingSingleDbContext context)
    : IRequestHandler<GetAllCalculationsByDepartmentQuery, IEnumerable<ListCalculationDTO>>
{
    public async Task<IEnumerable<ListCalculationDTO>> Handle(GetAllCalculationsByDepartmentQuery request, CancellationToken cancellationToken)
    {
        return await context.Calculation.AsNoTracking()
            .Where(x => x.ProjectId == request.ProjectId &&
                        (request.DepartmentId == null ||
                         x.SharesCalc.Any(s => s.UserId == request.UserId || s.DepartmentId == request.DepartmentId)))
            .Select(x => new ListCalculationDTO
            {
                Id = x.Id,
                Name = x.Name,
                Order = x.Order,
                IsPrivate = x.IsPrivate,
                TenderDeadline = x.TenderDeadline,
                TenderQA = x.TenderQA,
            }).ToListAsync(cancellationToken);
    }
}
