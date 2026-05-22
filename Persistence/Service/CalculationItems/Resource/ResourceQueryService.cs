using Application.Extension;
using Application.Feature.Calculation.Resource;
using Domain.Entities.Calculation;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems.Resource
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

            return [.. list.Select(x => x.MapToResourceListDTO())];
        }
    }
}
