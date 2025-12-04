using Application.Interfaces;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Application.Feature.Calculation.ResourceType.Queries;

public sealed record GetResourceSortQuery(int ResourceId) : IRequest<List<ResourceSortModel>>;

public sealed class GetResourceSortQueryHandler(IShardingSingleDbContext context)
    : IRequestHandler<GetResourceSortQuery, List<ResourceSortModel>>
{
    public async Task<List<ResourceSortModel>> Handle(GetResourceSortQuery request, CancellationToken cancellationToken)
    {
        return await context.ResourceSort
            .AsNoTracking()
            .Where(x => x.ResourceTypeId == request.ResourceId)
            .Select(x => new ResourceSortModel
            {
                BaseCost = x.Data.BaseCost,
                CapWaste = x.Data.CapWaste,
                ChangeFactor1 = x.Data.ChangeFactor1,
                ChangeFactor2 = x.Data.ChangeFactor2,
                CO2 = x.Data.CO2,
                Cost = x.Data.Cost,
                FixedQ = x.Data.FixedQ,
                Id = x.Id,
                IsVisible = x.IsVisible,
                Name = x.Name,
                Order = x.Order,
                Unit = x.Data.Unit,
                ResourceTypeId = x.ResourceTypeId,
                AccountId = x.AccountId
            })
            .ToListAsync(cancellationToken);
    }
}
