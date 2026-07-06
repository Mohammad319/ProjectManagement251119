using Application.Feature.Organisation.OrganisationCategory;
using Domain.DTO.Category;
using Domain.Entities.Organisation;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Organisation;
using System.ComponentModel.DataAnnotations;

namespace Persistence.Service.Organisation
{
    public sealed class OrganisationCategoryService(IDbContextFactoryTenant dbFactory) : IOrganisationCategoryService
    {
        public async Task<int> CreateAsync(PostOrganisationCategoryDTO dto, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            if (dto.CategoryId.HasValue)
            {
                var parent = await db.OrganisationCategory
                    .FirstOrDefaultAsync(x => x.Id == dto.CategoryId.Value, ct);

                if (parent == null || parent.ParentCategoryId.HasValue)
                    throw new ValidationException("Only one level of hierarchy is allowed.");
            }

            var entity = OrganisationCategoryEntity.Create(dto);
            db.OrganisationCategory.Add(entity);
            await db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(PutOrganisationCategoryDTO dto, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var entity = await db.OrganisationCategory
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (entity == null) return false;

            entity.Update(dto);
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            if (await db.OrganisationCategory.AnyAsync(x => x.ParentCategoryId == id, ct))
                return false;

            if (await db.Organisation.AnyAsync(x => x.OrganisationCategoryId == id, ct))
                return false;

            var entity = await db.OrganisationCategory
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity == null) return false;

            db.OrganisationCategory.Remove(entity);
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<List<ListOrganisationCategoryDTO>> GetAllAsync(CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            return await db.OrganisationCategory
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new ListOrganisationCategoryDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    ParentCategoryId = x.ParentCategoryId,
                    OrganisationCount = x.Organisations.Count
                })
                .ToListAsync(ct);
        }
    }
}
