using Application.Interfaces;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Application.Feature.Calculation.ResourceType.Queries
{
    public sealed record GetVisualCompensationQuery() : IRequest<ResourceFormDTO>;

    public class GetVisualResourcesQuery : IRequest<ResourceFormDTO>
    {
        public class GetResTypesAsListQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetVisualResourcesQuery, ResourceFormDTO>
        {
            public async Task<ResourceFormDTO> Handle(GetVisualResourcesQuery request, CancellationToken cancellationToken)
            {
                ResourceFormDTO result = new();
                result.ResourceTypes = await context.ResourceType.Where(x => x.IsVisible == true)
                    .AsNoTracking().OrderByDescending(x => x)
                    .Select(x => new ListResourceTypeDTO()
                    {
                        Id = x.Id,
                        AccountId = x.AccountId.Value,
                        ChangeFactor1 = x.Data.ChangeFactor1,
                        ChangeFactor2 = x.Data.ChangeFactor2,
                        BaseCost = x.Data.BaseCost,
                        Name = x.Name,
                        CapWaste = x.Data.CapWaste,
                        CO2 = x.Data.CO2,
                        Cost = x.Data.Cost,
                        FixedQ = x.Data.FixedQ,
                        Unit = x.Data.Unit,
                        Type = x.Type,
                        Order = x.Order,
                        IsVisible = x.IsVisible,
                        ResourcesSort = x.ResourcesSort.Select(rs => new ListResourceSortDTO
                        {
                            Id = rs.Id,
                            BaseCost = rs.Data.BaseCost,
                            Cost = rs.Data.Cost,
                            Name = rs.Name,
                            IsVisible = rs.IsVisible,
                            AccountId = rs.AccountId.Value,
                            ChangeFactor1 = rs.Data.ChangeFactor1,
                            ChangeFactor2 = rs.Data.ChangeFactor2,
                            CapWaste = rs.Data.CapWaste,
                            CO2 = rs.Data.CO2,
                            FixedQ = rs.Data.FixedQ,
                            Unit = rs.Data.Unit,
                            Order = rs.Order,
                        }).ToList(),
                    }).ToListAsync();

                result.Statues = await context.ResourceStatus
                    .AsNoTracking()
                    .OrderBy(x => x.Order)
                    .Select(x => new ListDTO
                    {
                        Id = x.Id,
                        Name = x.Name,
                    }).ToListAsync();

                result.AccountGroups = await context.AccountGroup.Select(x => new AccountGroupsListDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Accounts = x.Accounts.Select(a => new ListAccountDTO
                    {
                        Id = a.Id,
                        Name = a.Name
                    }).ToList()
                }).ToListAsync();
                return result;
            }
        }
    }
}
