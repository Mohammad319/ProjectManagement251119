using Application.Interfaces;
using ProjectManagement.Shared.DTO.App;

namespace Application.Feature.Application.Queries;

public sealed record GetCalcAppQuery(int CalcId) : IRequest<List<ApplicationValuesDTO>>;

public sealed class GetCalcAppQueryHandler(IApplicationService applicationService)
    : IRequestHandler<GetCalcAppQuery, List<ApplicationValuesDTO>>
{
    public async Task<List<ApplicationValuesDTO>> Handle(GetCalcAppQuery request, CancellationToken cancellationToken)
         => await applicationService.GetCalcAppAsync(request.CalcId, cancellationToken);
}
