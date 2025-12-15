using Application.Interfaces;
using Domain.Entities.Application;

namespace Application.Feature.Application.Queries;

public sealed record GetApplicationQuery(bool WithNoneVisible) : IRequest<List<ApplicationEntity>>;

public sealed class GetApplicationQueryHandler(IApplicationService applicationService)
    : IRequestHandler<GetApplicationQuery, List<ApplicationEntity>>
{
    public async Task<List<ApplicationEntity>> Handle(GetApplicationQuery request, CancellationToken cancellationToken)
         => await applicationService.GetApplicationQueryAsync(request.WithNoneVisible, cancellationToken);

}
