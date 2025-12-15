using Application.Extention;
using Application.Feature.Calculation.Resource;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
namespace Persistence.Service.CalculationItems
{
    public sealed class ResourceQueryService(ShardingSingleDbContext dataAccess) : IResourceQueryService
    {
        public async Task<List<ResourceListDTO>> GetByFilterAsync(
            FilterCalculationItemsDto filter,
            CancellationToken cancellationToken = default)
        {
            var query = dataAccess.Resources
                .AsNoTracking()
                .AsQueryable();

            query = ApplyBaseFilter(query, filter);
            query = ApplyOptionalFilter(query, filter);

            // نحتاج Include لبعض الـ navigation لو بتستخدمها في الماب
            query = query
                .Include(x => x.Account)
                .Include(x => x.Status)
                .Include(x => x.Opportunity)
                .Include(x => x.ResourceSort)
                .Include(x => x.ResourceType);

            var resultList = await query.ToListAsync(cancellationToken);

            // فلترة Unit على الـ Metadata في الذاكرة (لو Unit مخزنة في JSON)
            if (!string.IsNullOrWhiteSpace(filter.Unit))
            {
                resultList = resultList
                    .Where(x =>
                        !string.IsNullOrEmpty(x.Metadata.Unit) &&
                        x.Metadata.Unit.Contains(filter.Unit, StringComparison.CurrentCultureIgnoreCase))
                    .ToList();
            }

            // تحويل إلى DTO باستعمال الامتداد الموجود عندك
            return resultList
                .Select(x => x.MapToResourceListDTO())
                .ToList();
        }

        private static IQueryable<ResourceEntity> ApplyBaseFilter(
            IQueryable<ResourceEntity> query,
            FilterCalculationItemsDto filter)
        {
            if (filter.CalculationID > 0)
                return query.Where(x => x.Task.CalculationId == filter.CalculationID);

            if (filter.ProjectID.HasValue)
                return query.Where(x => x.Task.Calculation.ProjectId == filter.ProjectID);

            if (filter.FolderID.HasValue)
                return query.Where(x => x.Task.Calculation.Project.FolderId == filter.FolderID);

            return query;
        }

        private static IQueryable<ResourceEntity> ApplyOptionalFilter(
            IQueryable<ResourceEntity> query,
            FilterCalculationItemsDto filter)
        {
            if (filter.ResType.HasValue)
                query = query.Where(x => x.ResType == filter.ResType);

            if (filter.ResourceTypeId > 0)
                query = query.Where(x => x.ResourceTypeId == filter.ResourceTypeId);

            if (filter.ResourceSortId > 0)
                query = query.Where(x => x.ResourceSortId == filter.ResourceSortId);

            if (!string.IsNullOrWhiteSpace(filter.Name))
                query = query.Where(x => x.Name.Contains(filter.Name));

            if (filter.Account > 0)
                query = query.Where(x => x.AccountId == filter.Account);

            if (filter.AccountGroup > 0 && filter.Account == 0)
                query = query.Where(x => x.Account != null && x.Account.AccountGroupId == filter.AccountGroup);

            if (filter.Status > 0)
                query = query.Where(x => x.StatusId == filter.Status);

            return query;
        }
    }
}
