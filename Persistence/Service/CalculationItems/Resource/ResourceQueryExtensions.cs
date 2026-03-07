using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems.Resource
{
    internal static class ResourceQueryExtensions
    {
        internal static IQueryable<ResourceEntity> ApplyFilterSqlOnly(
            this IQueryable<ResourceEntity> query,
            FilterCalculationItemsDto filter)
        {
            if (filter.CalculationID > 0)
                query = query.Where(x => x.Task.CalculationId == filter.CalculationID);
            else if (filter.ProjectID.HasValue)
                query = query.Where(x => x.Task.Calculation.ProjectId == filter.ProjectID);
            else if (filter.FolderID.HasValue)
                query = query.Where(x => x.Task.Calculation.Project.FolderId == filter.FolderID);

            if (!string.IsNullOrWhiteSpace(filter.Unit))
            {
                var q = $"%{filter.Unit.Trim()}%";
                query = query.Where(x => x.Unit != null && EF.Functions.Like(x.Unit, q));
            }

            if (filter.ResType.HasValue)
                query = query.Where(x => x.ResType == filter.ResType);

            if (filter.ResourceTypeId > 0)
                query = query.Where(x => x.ResourceTypeId == filter.ResourceTypeId);

            if (filter.ResourceSortId > 0)
                query = query.Where(x => x.ResourceSortId == filter.ResourceSortId);

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var q = $"%{filter.Name.Trim()}%";
                query = query.Where(x => EF.Functions.Like(x.Name, q));
            }

            if (filter.Account > 0)
                query = query.Where(x => x.AccountId == filter.Account);

            if (filter.AccountGroup > 0 && filter.Account == 0)
                query = query.Where(x => x.Account != null && x.Account.AccountGroupId == filter.AccountGroup);

            if (filter.Status > 0)
                query = query.Where(x => x.StatusId == filter.Status);

            return query;
        }

        internal static IQueryable<ResourceEntity> IncludeResourceLookups(this IQueryable<ResourceEntity> query)
        {
            return query
                .Include(x => x.Account)
                .Include(x => x.Status)
                .Include(x => x.Opportunity)
                .Include(x => x.ResourceSort)
                .Include(x => x.ResourceType);
        }
    }
}
