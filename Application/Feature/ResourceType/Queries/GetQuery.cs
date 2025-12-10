using Application.Interfaces;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Application.Feature.Calculation.ResourceType.Queries;

public sealed record GetQuery(bool IsVisible) : IRequest<List<ResourceTypeModel>>;

public sealed class GetQueryHandler(IShardingSingleDbContext context): IRequestHandler<GetQuery, List<ResourceTypeModel>>
{
    public async Task<List<ResourceTypeModel>> Handle(GetQuery request, CancellationToken cancellationToken)
    {
        return await context.ResourceTypes
            .AsNoTracking()
            .Where(x => x.IsVisible == request.IsVisible)
            .Select(x => new ResourceTypeModel
            {
                BaseCost = x.Metadata.BaseCost,
                CapWaste = x.Metadata.CapWaste,
                ChangeFactor1 = x.Metadata.ChangeFactor1,
                ChangeFactor2 = x.Metadata.ChangeFactor2,
                CO2 = x.Metadata.CO2,
                Cost = x.Metadata.Cost,
                FixedQ = x.Metadata.FixedQ,
                Unit = x.Metadata.Unit,
                IsVisible = request.IsVisible,
                Order = x.SortOrder,
                Id = x.Id,
                Name = x.Name,
                Type = x.Kind,
                AccountId = x.AccountId
            })
            .ToListAsync(cancellationToken);
    }
}
