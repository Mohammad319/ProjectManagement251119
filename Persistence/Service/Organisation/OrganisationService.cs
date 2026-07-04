using Application.Feature.Organisation.Organisation;
using Application.Mapping.Organisation;
using Domain.Entities.Organisation;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Organisation;

namespace Persistence.Service.Organisation
{
    public sealed class OrganisationService(IDbContextFactoryTenant dbFactory) : IOrganisationService
    {
        public async Task<int> CreateAsync(PostOrganisationDTO dto, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var entity = OrganisationEntity.Create(dto);
            db.Organisation.Add(entity);
            await db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, PostOrganisationDTO dto, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var entity = await db.Organisation
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity == null)
                return false;

            entity.Update(dto);
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var isUsedByTenders = await db.Tenders
                .AsNoTracking()
                .AnyAsync(x => x.OrganisationId == id, ct);

            var isUsedByProjects = await db.Projects
                .AsNoTracking()
                .AnyAsync(x => x.OrganisationId == id, ct);

            var isUsedByCalculations = await db.Calculations
                .AsNoTracking()
                .AnyAsync(x => x.OrganisationId == id, ct);

            if (isUsedByTenders || isUsedByProjects || isUsedByCalculations)
                return false;

            var entity = await db.Organisation
                .Include(x => x.Offers)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity == null)
                return false;

            foreach (var offer in entity.Offers)
                offer.Update(null, offer.GetMetadataSnapshot(), offer.Comment);

            db.Organisation.Remove(entity);
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<OrganisationDetailsDTO?> GetDetailsAsync(int id, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var entity = await db.Organisation
                .AsNoTracking()
                .Include(x => x.OrganisationCategory).ThenInclude(c => c.ParentCategory)
                .Include(x => x.OrganisationType)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            return entity?.ToDetailsDto();
        }

        public async Task<PostOrganisationDTO?> GetPostAsync(int id, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var entity = await db.Organisation
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            return entity?.ToPostDto();
        }

        public async Task<List<ShortListOrganisationDTO>> GetByCategoryAsync(int categoryId, bool isVisible, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            return await db.Organisation
                .AsNoTracking()
                .Where(x => x.OrganisationCategoryId == categoryId && x.IsVisible == isVisible)
                .OrderBy(x => x.Name)
                .Select(x => new ShortListOrganisationDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Category = x.OrganisationCategory != null && x.OrganisationCategory.ParentCategory != null ? x.OrganisationCategory.ParentCategory.Name : string.Empty,
                    SubCategory = x.OrganisationCategory != null ? x.OrganisationCategory.Name : string.Empty,
                    Type = x.OrganisationType != null ? x.OrganisationType.Name : string.Empty
                })
                .ToListAsync(ct);
        }

        public async Task<List<ListDTO>> GetVisibleOrIdAsync(int? id, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            return await db.Organisation
                .AsNoTracking()
                .Where(x => x.IsVisible || (id.HasValue && x.Id == id))
                .OrderBy(x => x.Name)
                .Select(x => new ListDTO { Id = x.Id, Name = x.Name })
                .ToListAsync(ct);
        }

        public async Task<List<ListDTO>> GetAsListAsync(CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            return await db.Organisation
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new ListDTO { Id = x.Id, Name = x.Name })
                .ToListAsync(ct);
        }

        public async Task<List<OrganisationRowDTO>> GetRowsAsync(bool includeArchived, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var query = db.Organisation
                .AsNoTracking()
                .Include(x => x.OrganisationCategory).ThenInclude(c => c.ParentCategory)
                .Include(x => x.OrganisationType)
                .AsQueryable();

            if (!includeArchived)
                query = query.Where(x => x.IsVisible);

            var entities = await query.OrderBy(x => x.Name).ToListAsync(ct);

            // Posts referenced by a projekt/kalkyl/anbud may not be deleted (only archived). Same rule
            // as DeleteAsync, gathered as id-sets so the whole list is marked in three queries, not N.
            var used = new HashSet<int>();
            used.UnionWith(await db.Tenders.AsNoTracking()
                .Select(x => x.OrganisationId).Distinct().ToListAsync(ct));
            used.UnionWith(await db.Projects.AsNoTracking()
                .Where(x => x.OrganisationId != null).Select(x => x.OrganisationId!.Value).Distinct().ToListAsync(ct));
            used.UnionWith(await db.Calculations.AsNoTracking()
                .Where(x => x.OrganisationId != null).Select(x => x.OrganisationId!.Value).Distinct().ToListAsync(ct));

            var rows = new List<OrganisationRowDTO>(entities.Count);
            foreach (var x in entities)
            {
                var category = x.OrganisationCategory;
                var hasParent = category?.ParentCategory is not null;

                var md = x.GetMetadataSnapshot();
                var address = md.Address?.FirstOrDefault();

                rows.Add(new OrganisationRowDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    MainGroupId = hasParent ? category!.ParentCategoryId : category?.Id,
                    MainGroup = (hasParent ? category!.ParentCategory!.Name : category?.Name) ?? string.Empty,
                    SubCategoryId = hasParent ? category!.Id : null,
                    SubCategory = hasParent ? category!.Name : string.Empty,
                    TypeId = x.OrganisationTypeId,
                    Type = x.OrganisationType?.Name ?? string.Empty,
                    OrganisationNumber = string.IsNullOrWhiteSpace(md.PIDNumber) ? md.IDNumber : md.PIDNumber,
                    Email = md.Email,
                    Phone = string.IsNullOrWhiteSpace(md.Phone) ? md.Mobile : md.Phone,
                    City = address?.City ?? string.Empty,
                    Country = address?.Country ?? string.Empty,
                    Status = md.Status,
                    IsVisible = x.IsVisible,
                    IsUsed = used.Contains(x.Id),
                    UpdatedAt = x.UpdatedAt ?? x.CreatedAt
                });
            }

            return rows;
        }

        public async Task<bool> SetVisibilityAsync(int id, bool visible, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var entity = await db.Organisation.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null)
                return false;

            entity.SetVisibility(visible);
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<List<ListDTO>> FindSimilarByNameAsync(string name, CancellationToken ct = default)
        {
            var trimmed = (name ?? string.Empty).Trim();
            if (trimmed.Length < 2)
                return [];

            await using var db = await dbFactory.CreateDbContextAsync(ct);

            return await db.Organisation
                .AsNoTracking()
                .Where(x => x.Name.Contains(trimmed))
                .OrderBy(x => x.Name)
                .Take(10)
                .Select(x => new ListDTO { Id = x.Id, Name = x.Name })
                .ToListAsync(ct);
        }
    }
}
