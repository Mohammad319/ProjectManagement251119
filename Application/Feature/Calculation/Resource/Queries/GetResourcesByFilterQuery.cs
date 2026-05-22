using Application.Extension;
using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;
namespace Application.Feature.Calculation.Resource.Queries;

public sealed record GetResourcesByFilterQuery(FilterCalculationItemsDto Filter)
    : IRequest<List<ResourceListDTO>>;

public sealed class GetResourcesByFilterQueryHandler(IResourceQueryService service)
        : IRequestHandler<GetResourcesByFilterQuery, List<ResourceListDTO>>
{
    public Task<List<ResourceListDTO>> Handle(
        GetResourcesByFilterQuery request,
        CancellationToken cancellationToken)
    {
        return service.GetByFilterAsync(request.Filter, cancellationToken);
    }
}