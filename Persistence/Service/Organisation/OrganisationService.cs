using Application.Feature.Organisation.Organisation;
using Domain.Entities.Organisation;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Organisation;

namespace Persistence.Service.Organisation
{
    public sealed class OrganisationService(IDbContextFactory dbFactory) : IOrganisationService
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

            var entity = await db.Organisation.FindAsync([id], ct);
            if (entity == null) return false;

            entity.Update(dto);
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var entity = await db.Organisation
                .Include(x => x.Offers)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity == null) return false;

            foreach (var offer in entity.Offers)
                offer.Update(null, offer.Metadata, offer.Comment);

            db.Organisation.Remove(entity);
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<OrganisationDetailsDTO?> GetDetailsAsync(int id, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var x = await db.Organisation
                .AsNoTracking()
                .Include(x => x.OrganisationCategory).ThenInclude(c => c.ParentCategory)
                .Include(x => x.OrganisationType)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (x == null) return null;

            return new OrganisationDetailsDTO
            {
                Name = x.Name,
                Category = x.OrganisationCategory?.ParentCategory?.Name,
                SubCategory = x.OrganisationCategory?.Name,
                Type = x.OrganisationType?.Name,
                Address = x.Metadata.Address,
                Contacts = x.Metadata.Contacts,
                Email = x.Metadata.Email,
                Mobile = x.Metadata.Mobile,
                Notes = x.Metadata.Notes,
                Rating = x.Metadata.Rating,
                Status = x.Metadata.Status,
                URL = x.Metadata.URL
            };
        }

        public async Task<PostOrganisationDTO?> GetPostAsync(int id, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var x = await db.Organisation
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (x == null) return null;

            return new PostOrganisationDTO
            {
                Name = x.Name,
                CategoryId = x.OrganisationCategoryId,
                OrganisationTypeID = x.OrganisationTypeId,
                IsVisible = x.IsVisible,
                Address = x.Metadata.Address,
                Contacts = x.Metadata.Contacts,
                Email = x.Metadata.Email,
                Notes = x.Metadata.Notes,
                Rating = x.Metadata.Rating,
                URL = x.Metadata.URL
            };
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
                    Category = x.OrganisationCategory.ParentCategory.Name,
                    SubCategory = x.OrganisationCategory.Name,
                    Type = x.OrganisationType.Name
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
    }
}
