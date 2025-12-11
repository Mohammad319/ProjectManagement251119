using Application.Feature.Calculation.TaskStatus;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
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
    }


}
