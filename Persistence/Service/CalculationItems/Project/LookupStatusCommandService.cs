using Application.Feature.General;
using Domain.Entities.Calculation;
using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.General;

namespace Persistence.Service.CalculationItems.Project
{
    public sealed class LookupStatusCommandService<TS>(IDbContextFactoryTenant dbFactory) : ILookupStatusCommandService<TS>
        where TS : class, IListOrderDTO, new()
    {
        public async Task<int> CreateAsync(PostTaskStatusDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            // هنا نستخدم TS بدل TaskStatusEntity
            var entity = new TS();
            entity.Update(dto.Name, dto.Color, dto.Order, dto.IsVisible);
            ApplyStatusSettings(entity, dto);

            context.Set<TS>().Add(entity);
            await context.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, PostTaskStatusDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var set =context.Set<TS>();

            var entity = await set.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity == null)
                return false;

            entity.Update(dto.Name, dto.Color, dto.Order, dto.IsVisible);
            ApplyStatusSettings(entity, dto);

            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var set =context.Set<TS>();

            var entity = await set.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity == null)
                return false;

            set.Remove(entity);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IReadOnlyList<LookupAdminListItemDto>> GetAllListAsync(CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Set<TS>()
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .Select(x => new LookupAdminListItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Color = x.Color,
                    SortOrder = x.SortOrder,
                    IsVisible = x.IsVisible
                })
                .ToListAsync(ct);
        }

        // -------------------------------------------------
        // GetAllAsync: يرجع كل الكيانات كما هي
        // -------------------------------------------------
        public async Task<List<TS>> GetAllAsync(CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Set<TS>()
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);
        }

        // -------------------------------------------------
        // GetVisualAsync: للـ dropdowns و الـ UI
        // -------------------------------------------------
        public async Task<IEnumerable<ListOrderDTO>> GetVisualAsync(int? id, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var query = context.Set<TS>().AsNoTracking();

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

        public async Task<bool> MoveAsync(int id, bool moveUp, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var all = await context.Set<TS>().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(ct);
            var item = all.FirstOrDefault(x => x.Id == id);
            if (item is null) return false;

            var sameGroup = all.Where(x => x.IsVisible == item.IsVisible).ToList();
            var groupIdx = sameGroup.FindIndex(x => x.Id == id);
            var swapIdx = moveUp ? groupIdx - 1 : groupIdx + 1;
            if (swapIdx < 0 || swapIdx >= sameGroup.Count) return false;

            // Swap by position in list, then normalize SortOrder.
            // Direct SortOrder-value swap fails when two adjacent items share the same value.
            (sameGroup[groupIdx], sameGroup[swapIdx]) = (sameGroup[swapIdx], sameGroup[groupIdx]);

            for (var i = 0; i < sameGroup.Count; i++)
                sameGroup[i].Update(sameGroup[i].Name, sameGroup[i].Color, i, sameGroup[i].IsVisible);

            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<int> CountCalculationsByStatusAsync(int id, CancellationToken ct = default)
        {
            if (typeof(TS) != typeof(StatusEntity)) return 0;
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Set<CalculationEntity>()
                .Where(x => x.StatusId == id)
                .CountAsync(ct);
        }

        private static void ApplyStatusSettings(TS entity, PostTaskStatusDTO dto)
        {
            if (entity is StatusEntity status)
            {
                status.SetApprovalSettings(
                    dto.IsApprovalStatus,
                    dto.LocksCalculation,
                    dto.AllowsProductionCalculation);
                status.SetHitRateSettings(
                    dto.CountsAsSubmittedBid,
                    dto.CountsAsWonBid,
                    dto.CountsAsLostBid);
            }
        }

    }


}
