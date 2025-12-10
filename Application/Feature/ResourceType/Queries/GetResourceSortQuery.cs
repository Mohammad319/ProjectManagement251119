using Application.Interfaces;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Application.Feature.Calculation.ResourceType.Queries;

public sealed record GetResourceSortQuery(int ResourceId) : IRequest<List<ResourceSortModel>>;

public sealed class GetResourceSortQueryHandler(IShardingSingleDbContext context)
    : IRequestHandler<GetResourceSortQuery, List<ResourceSortModel>>
{
    public async Task<List<ResourceSortModel>> Handle(GetResourceSortQuery request, CancellationToken cancellationToken)
    {
        return await context.ResourceSorts
            .AsNoTracking()
            .Where(x => x.ResourceTypeId == request.ResourceId)
            .Select(x => new ResourceSortModel
            {
                BaseCost = x.Metadata.BaseCost,
                CapWaste = x.Metadata.CapWaste,
                ChangeFactor1 = x.Metadata.ChangeFactor1,
                ChangeFactor2 = x.Metadata.ChangeFactor2,
                CO2 = x.Metadata.CO2,
                Cost = x.Metadata.Cost,
                FixedQ = x.Metadata.FixedQ,
                Id = x.Id,
                IsVisible = x.IsVisible,
                Name = x.Name,
                Order = x.SortOrder,
                Unit = x.Metadata.Unit,
                ResourceTypeId = x.ResourceTypeId,
                AccountId = x.AccountId
            })
            .ToListAsync(cancellationToken);
    }
}
