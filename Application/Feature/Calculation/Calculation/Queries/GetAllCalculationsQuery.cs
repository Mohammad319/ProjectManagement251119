using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;
using System;

namespace Application.Feature.Calculation.Calculation.Queries;

public sealed record GetAllCalculationsQuery(Guid ProjectId, int UserId, int? DepartmentId)
    : IRequest<IEnumerable<ListCalculationDTO>>;

public sealed class GetAllCalculationsQueryHandler(IShardingSingleDbContext context)
    : IRequestHandler<GetAllCalculationsQuery, IEnumerable<ListCalculationDTO>>
{
    public async Task<IEnumerable<ListCalculationDTO>> Handle(GetAllCalculationsQuery request, CancellationToken cancellationToken)
    {
        return await context.Calculation
            .AsNoTracking()
            .Where(x =>
                x.ProjectId == request.ProjectId &&
                (!request.DepartmentId.HasValue || x.Project.Folder.DepartmentId == request.DepartmentId) &&
                (!x.IsPrivate || x.CreatedBy == request.UserId))
            .Select(x => new ListCalculationDTO
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Order = x.SortOrder,
                TenderDeadline = x.TenderDeadline,
                TenderQA = x.TenderQA,
                IsPrivate = x.IsPrivate,
                EndDate = x.EndDate,
                StartDate = x.StartDate,
                Status = x.Status.Name
            })
            .ToListAsync(cancellationToken);
    }
}
