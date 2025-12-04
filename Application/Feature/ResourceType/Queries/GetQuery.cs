using Application.Interfaces;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Application.Feature.Calculation.ResourceType.Queries;

public sealed record GetQuery(bool IsVisible) : IRequest<List<ResourceTypeModel>>;

public sealed class GetQueryHandler(IShardingSingleDbContext context): IRequestHandler<GetQuery, List<ResourceTypeModel>>
{
    public async Task<List<ResourceTypeModel>> Handle(GetQuery request, CancellationToken cancellationToken)
    {
        return await context.ResourceType
            .AsNoTracking()
            .Where(x => x.IsVisible == request.IsVisible)
            .Select(x => new ResourceTypeModel
            {
                BaseCost = x.Data.BaseCost,
                CapWaste = x.Data.CapWaste,
                ChangeFactor1 = x.Data.ChangeFactor1,
                ChangeFactor2 = x.Data.ChangeFactor2,
                CO2 = x.Data.CO2,
                Cost = x.Data.Cost,
                FixedQ = x.Data.FixedQ,
                Unit = x.Data.Unit,
                IsVisible = request.IsVisible,
                Order = x.Order,
                Id = x.Id,
                Name = x.Name,
                Type = x.Type,
                AccountId = x.AccountId
            })
            .ToListAsync(cancellationToken);
    }
}
