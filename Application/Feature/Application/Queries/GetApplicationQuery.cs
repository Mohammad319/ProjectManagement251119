using Application.Interfaces;
using ProjectManagement.Shared.DTO.App;

namespace Application.Feature.Application.Queries;

public sealed record GetApplicationQuery(bool WithNoneVisible) : IRequest<List<ApplicationDTO>>;

public sealed class GetApplicationQueryHandler(IApplicationService applicationService)
    : IRequestHandler<GetApplicationQuery, List<ApplicationDTO>>
{
    public async Task<List<ApplicationDTO>> Handle(GetApplicationQuery request, CancellationToken cancellationToken)
         => await applicationService.GetApplicationQueryAsync(request.WithNoneVisible, cancellationToken);
}
