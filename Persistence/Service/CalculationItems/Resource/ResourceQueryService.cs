using Application.Extention;
using Application.Feature.Calculation.Resource;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems
{
    public sealed class ResourceQueryService(IDbContextFactoryTenant dbFactory) : IResourceQueryService
    {
        public async Task<List<ResourceListDTO>> GetByFilterAsync(
            FilterCalculationItemsDto filter,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            IQueryable<ResourceEntity> query = context.Resources
                .AsNoTracking()
                .ApplyFilterSqlOnly(filter)
                .IncludeResourceLookups(); // لو الماب يعتمد على navigation properties

            // ✅ تحميل بعد تقليل النتائج قدر الإمكان من SQL
            var list = await query.ToListAsync(ct);

            // ✅ فلترة Unit في الذاكرة لأن Unit داخل JSON
            if (!string.IsNullOrWhiteSpace(filter.Unit))
            {
                list = [.. list
                    .Where(x =>
                        !string.IsNullOrEmpty(x.Metadata.Unit) &&
                        x.Metadata.Unit.Contains(filter.Unit, StringComparison.CurrentCultureIgnoreCase))];
            }

            return list.Select(x => x.MapToResourceListDTO()).ToList();
        }
    }

    internal static class ResourceQueryExtensions
    {
        // هذه الفلاتر فقط اللي نضمن أنها تترجم SQL
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
