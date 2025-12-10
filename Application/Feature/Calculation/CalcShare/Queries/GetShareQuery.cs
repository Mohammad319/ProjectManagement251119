using Application.Interfaces;
using Application.Interfaces.Context;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.CalcShare.Queries;

public sealed record GetShareQuery(int Id, int? DepartmentId, int UserId) : IRequest<IEnumerable<ListShareCalcDTO>>;

public sealed class GetShareQueryHandler(IShardingSingleDbContext _context)
    : IRequestHandler<GetShareQuery, IEnumerable<ListShareCalcDTO>>
{
    public async Task<IEnumerable<ListShareCalcDTO>> Handle(GetShareQuery request, CancellationToken cancellationToken)
    {
        return await _context.ShareCalc
            .Where(x => x.CalculationId == request.Id &&
                        x.Calculation.Project.Folder.DepartmentId == request.DepartmentId)
            .Select(x => new ListShareCalcDTO
            {
                Id = x.Id,
                DepartmentId = x.DepartmentId,
                UserId = x.CreatedBy,
                User = x.CreatedAtUser.FirstName + " " + x.CreatedAtUser.LastName,
                Tap1 = x.Metadata.Tap1,
                Tap2 = x.Metadata.Tap2,
                Tap3 = x.Metadata.Tap3,
                Tap4 = x.Metadata.Tap4,
                Tap5 = x.Metadata.Tap5,
                Tap6 = x.Metadata.Tap6,
                Department = x.Department.Name
            }).ToListAsync(cancellationToken);
    }
}
