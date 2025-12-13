using Application.Feature.General;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using ProjectManagement.Shared.DTO.General;

namespace Persistence.Service.CalculationItems.Project
{
    public sealed class LookupStatusQueryService<TEntity>(ShardingSingleDbContext context) : ILookupStatusQueryService<TEntity>
        where TEntity : class, IListOrderDTO
    {
        private DbSet<TEntity> Set => context.Set<TEntity>();

        // -------------------------------------------------
        // GetAllAsync: يرجع كل الكيانات كما هي
        // -------------------------------------------------
        public async Task<List<TEntity>> GetAllAsync(CancellationToken ct = default)
        {
            return await Set
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);
        }

        // -------------------------------------------------
        // GetVisualAsync: للـ dropdowns و الـ UI
        // -------------------------------------------------
        public async Task<IEnumerable<ListOrderDTO>> GetVisualAsync(int? id, CancellationToken ct = default)
        {
            var query = Set.AsNoTracking();

            if (id.HasValue)
            {
                int targetId = id.Value;
                query = query.Where(x => x.IsVisible || x.Id == targetId);
            }
            else
            {
                query = query.Where(x => x.IsVisible);
            }

            return await query
                .OrderBy(x => x.SortOrder)
                .Select(x => new ListOrderDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    SortOrder = x.SortOrder
                })
                .ToListAsync(ct);
        }
    }

}
