using Application.Interfaces;
using Domain.Entities.Application;
namespace Application.Feature.Application.Queries;

public sealed record GetCalcAppQuery(int CalcId) : IRequest<IEnumerable<ApplicationValuesEntity>>;

public sealed class GetCalcAppQueryHandler(IApplicationService applicationService)
    : IRequestHandler<GetCalcAppQuery, IEnumerable<ApplicationValuesEntity>>
{
    public async Task<IEnumerable<ApplicationValuesEntity>> Handle(GetCalcAppQuery request, CancellationToken cancellationToken)
         => await applicationService.GetCalcAppAsync(request.CalcId, cancellationToken);
}
