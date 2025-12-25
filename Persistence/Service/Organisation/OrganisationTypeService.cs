using Application.Feature.Organisation.OrganisationType;
using Domain.DTO.Category;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Organisation;

namespace Persistence.Service.Organisation
{
    public sealed class OrganisationTypeService(Factory.IDbContextFactory dbFactory) : IOrganisationTypeService
    {
        // ---------------- Commands ----------------

        public async Task<int> CreateAsync(PostOrganisationTypeDTO dto, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var entity = OrganisationTypeEntity.Create(dto);
            db.OrganisationType.Add(entity);
            await db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, PostOrganisationTypeDTO dto, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var entity = await db.OrganisationType.FindAsync(id, ct);
            if (entity == null) return false;

            entity.Update(dto);
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var entity = await db.OrganisationType
                .Include(x => x.Organisations)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity == null) return false;

            if (entity.Organisations.Any())
                return false; // ممنوع الحذف إذا مرتبط بمنظمات

            db.OrganisationType.Remove(entity);
            await db.SaveChangesAsync(ct);
            return true;
        }

        // ---------------- Queries ----------------

        public async Task<List<ListOrganisationTypeDTO>> GetAllAsync(bool isVisible, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            return await db.OrganisationType
                .AsNoTracking()
                .Where(x => x.IsVisible == isVisible)
                .OrderBy(x => x.Name)
                .Select(x => new ListOrganisationTypeDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsVisible = x.IsVisible
                })
                .ToListAsync(ct);
        }

        public async Task<List<ListDTO>> GetAsListAsync(int? typeId, int? organisationId, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var q = db.OrganisationType.AsNoTracking();

            if (typeId.HasValue)
                q = q.Where(x => x.IsVisible || x.Id == typeId);

            if (organisationId.HasValue)
                q = q.Where(x => x.IsVisible || x.Organisations.Any(o => o.Id == organisationId));

            if (!typeId.HasValue && !organisationId.HasValue)
                q = q.Where(x => x.IsVisible);

            return await q
                .OrderBy(x => x.Name)
                .Select(x => new ListDTO { Id = x.Id, Name = x.Name })
                .ToListAsync(ct);
        }
    }
}
