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
                result.ResourceTypes = await context.ResourceTypes.Where(x => x.IsVisible == true)
                    .AsNoTracking().OrderByDescending(x => x)
                    .Select(x => new ListResourceTypeDTO()
                    {
                        Id = x.Id,
                        AccountId = x.AccountId.Value,
                        ChangeFactor1 = x.Metadata.ChangeFactor1,
                        ChangeFactor2 = x.Metadata.ChangeFactor2,
                        BaseCost = x.Metadata.BaseCost,
                        Name = x.Name,
                        CapWaste = x.Metadata.CapWaste,
                        CO2 = x.Metadata.CO2,
                        Cost = x.Metadata.Cost,
                        FixedQ = x.Metadata.FixedQ,
                        Unit = x.Metadata.Unit,
                        Type = x.Kind,
                        Order = x.SortOrder,
                        IsVisible = x.IsVisible,
                        ResourcesSort = x.ResourcesSort.Select(rs => new ListResourceSortDTO
                        {
                            Id = rs.Id,
                            BaseCost = rs.Metadata.BaseCost,
                            Cost = rs.Metadata.Cost,
                            Name = rs.Name,
                            IsVisible = rs.IsVisible,
                            AccountId = rs.AccountId.Value,
                            ChangeFactor1 = rs.Metadata.ChangeFactor1,
                            ChangeFactor2 = rs.Metadata.ChangeFactor2,
                            CapWaste = rs.Metadata.CapWaste,
                            CO2 = rs.Metadata.CO2,
                            FixedQ = rs.Metadata.FixedQ,
                            Unit = rs.Metadata.Unit,
                            Order = rs.SortOrder,
                        }).ToList(),
                    }).ToListAsync();

                result.Statues = await context.ResourceStatus
                    .AsNoTracking()
                    .OrderBy(x => x.SortOrder)
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
