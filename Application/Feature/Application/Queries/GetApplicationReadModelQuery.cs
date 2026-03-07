using Application.Interfaces;

namespace Application.Feature.Application.Queries;

public sealed record GetApplicationListQuery(bool WithNoneVisible) : IRequest<IReadOnlyList<ApplicationListItemDto>>;

public sealed class GetApplicationListQueryHandler(IApplicationService applicationService)
    : IRequestHandler<GetApplicationListQuery, IReadOnlyList<ApplicationListItemDto>>
{
    public Task<IReadOnlyList<ApplicationListItemDto>> Handle(GetApplicationListQuery request, CancellationToken cancellationToken)
        => applicationService.GetApplicationListAsync(request.WithNoneVisible, cancellationToken);
}

public sealed record GetCalcAppListQuery(int CalcId) : IRequest<IReadOnlyList<ApplicationValueListItemDto>>;

public sealed class GetCalcAppListQueryHandler(IApplicationService applicationService)
    : IRequestHandler<GetCalcAppListQuery, IReadOnlyList<ApplicationValueListItemDto>>
{
    public Task<IReadOnlyList<ApplicationValueListItemDto>> Handle(GetCalcAppListQuery request, CancellationToken cancellationToken)
        => applicationService.GetCalcAppListAsync(request.CalcId, cancellationToken);
}
