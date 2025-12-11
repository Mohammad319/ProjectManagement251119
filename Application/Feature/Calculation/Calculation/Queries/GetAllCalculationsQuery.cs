using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;
using System;

namespace Application.Feature.Calculation.Calculation.Queries;

public sealed record GetAllCalculationsQuery(Guid ProjectId, int UserId, int? DepartmentId)
    : IRequest<IEnumerable<ListCalculationDTO>>;
public sealed class GetAllCalculationsQueryHandler(ICalculationQueryService service)
        : IRequestHandler<GetAllCalculationsQuery, IEnumerable<ListCalculationDTO>>
{
    public Task<IEnumerable<ListCalculationDTO>> Handle(GetAllCalculationsQuery request, CancellationToken ct)
        => service.GetAllAsync(request.ProjectId, request.UserId, request.DepartmentId, ct);
}
