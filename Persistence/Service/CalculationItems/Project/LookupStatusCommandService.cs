using Application.Feature.General;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.General;

namespace Persistence.Service.CalculationItems.Project
{
    public sealed class LookupStatusCommandService<TS>(ShardingSingleDbContext db) : ILookupStatusCommandService<TS>
        where TS : class, IListOrderDTO, new()
    {
        public async Task<int> CreateAsync(PostTaskStatusDTO dto, CancellationToken ct = default)
        {
            // هنا نستخدم TS بدل TaskStatusEntity
            var entity = new TS();
            entity.Update(dto.Name, dto.Color, dto.Order, dto.IsVisible);

            db.Set<TS>().Add(entity);
            await db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, PostTaskStatusDTO dto, CancellationToken ct = default)
        {
            var set = db.Set<TS>();

            var entity = await set.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity == null)
                return false;

            entity.Update(dto.Name, dto.Color, dto.Order, dto.IsVisible);

            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var set = db.Set<TS>();

            var entity = await set.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity == null)
                return false;

            set.Remove(entity);
            await db.SaveChangesAsync(ct);
            return true;
        }


        private DbSet<TS> Set => db.Set<TS>();

        // -------------------------------------------------
        // GetAllAsync: يرجع كل الكيانات كما هي
        // -------------------------------------------------
        public async Task<List<TS>> GetAllAsync(CancellationToken ct = default)
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
