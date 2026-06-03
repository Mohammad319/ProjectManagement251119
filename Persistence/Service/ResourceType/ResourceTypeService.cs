using Application.Feature.ResourceType;
using Application.Mapping.ResourceType;
using Domain.Entities.ResourceType;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using Persistence.Service.Lookup;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Persistence.Service.ResourceType
{
    public sealed class ResourceTypeService(
        IDbContextFactoryTenant dbFactory,
        LookupCacheService lookupCache) : IResourceTypeService
    {
        public async Task<int> CreateTypeAsync(PostResourceTypeDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (!await ValidateAccountReferenceAsync(context, dto.AccountId, ct))
                return 0;

            var max = await context.ResourceTypes.MaxAsync(x => (int?)x.SortOrder, ct) ?? 0;
            var entity = ResourceTypeEntity.Create(dto, max + 100);

            context.ResourceTypes.Add(entity);
            await context.SaveChangesAsync(ct);
            await lookupCache.InvalidateAsync<ResourceTypeEntity>(ct);
            return entity.Id;
        }

        public async Task<bool> UpdateTypeAsync(int id, PostResourceTypeDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (!await ValidateAccountReferenceAsync(context, dto.AccountId, ct))
                return false;

            var entity = await context.ResourceTypes
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity == null) return false;

            entity.Update(dto);
            if (dto.Order >= 0)
                entity.UpdateOrder(dto.Order);
            await context.SaveChangesAsync(ct);
            await lookupCache.InvalidateAsync<ResourceTypeEntity>(ct);
            return true;
        }

        public async Task<bool> DeleteTypeAsync(int id, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.ResourceTypes
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity == null) return false;

            var isInUse = await context.ResourceSorts.AnyAsync(x => x.ResourceTypeId == id, ct)
                          || await context.Resources.AnyAsync(x => x.ResourceTypeId == id, ct);
            if (isInUse) return false;

            context.ResourceTypes.Remove(entity);

            try
            {
                await context.SaveChangesAsync(ct);
                await lookupCache.InvalidateAsync<ResourceTypeEntity>(ct);
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

        public async Task<int> CreateSortAsync(int resourceTypeId, PostResourceSortDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            bool typeExists = await context.ResourceTypes.AnyAsync(x => x.Id == resourceTypeId, ct);
            if (!typeExists) return 0;

            if (!await ValidateAccountReferenceAsync(context, dto.AccountId, ct))
                return 0;

            var max = await context.ResourceSorts
                .Where(x => x.ResourceTypeId == resourceTypeId)
                .MaxAsync(x => (int?)x.SortOrder, ct) ?? 0;

            var entity = Domain.Entities.ResourceType.ResourceSortEntity.Create(dto, resourceTypeId, max + 100);

            context.ResourceSorts.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> UpdateSortAsync(int id, PostResourceSortDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (!await ValidateAccountReferenceAsync(context, dto.AccountId, ct))
                return false;

            var entity = await context.ResourceSorts
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity == null) return false;

            entity.Update(dto);
            if (dto.Order >= 0)
                entity.UpdateOrder(dto.Order);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteSortAsync(int id, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.ResourceSorts
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity == null) return false;

            if (await context.Resources.AnyAsync(x => x.ResourceSortId == id, ct))
                return false;

            context.ResourceSorts.Remove(entity);

            try
            {
                await context.SaveChangesAsync(ct);
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

        public async Task<List<ResourceTypeModel>> GetTypesAsync(bool isVisible, CancellationToken ct = default)
        {
            var cacheKey = isVisible ? "visible" : "all";
            var entities = await lookupCache.GetAsync<ResourceTypeEntity>(
                cacheKey,
                db => db.ResourceTypes
                    .AsNoTracking()
                    .Where(x => x.IsVisible == isVisible)
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync(ct),
                ct);

            return entities.Select(x => x.ToModel()).ToList();
        }

        public async Task<List<ResourceSortModel>> GetSortsAsync(int resourceTypeId, CancellationToken ct = default)
        {
            var entities = await lookupCache.GetAsync<ResourceSortEntity>(
                $"type_{resourceTypeId}",
                db => db.ResourceSorts
                    .AsNoTracking()
                    .Where(x => x.ResourceTypeId == resourceTypeId)
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync(ct),
                ct);

            return entities.Select(x => x.ToModel()).ToList();
        }

        public async Task<ResourceFormDTO> GetVisualFormAsync(CancellationToken ct = default)
        {
            var result = new ResourceFormDTO();
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var resourceTypes = await context.ResourceTypes
                .AsNoTracking()
                .Include(x => x.ResourcesSort)
                .Where(x => x.IsVisible)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            result.ResourceTypes = resourceTypes.Select(x => x.ToListDto()).ToList();

            result.Statues = await context.ResourceStatus
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .Select(x => new ListDTO { Id = x.Id, Name = x.Name })
                .ToListAsync(ct);

            result.AccountGroups = await context.AccountGroup
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new AccountGroupsListDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Accounts = x.Accounts
                        .OrderBy(a => a.Code)
                        .Select(a => new ProjectManagement.Shared.DTO.Account.ListAccountDTO
                        {
                            Id = a.Id,
                            Name = a.Name
                        })
                        .ToList()
                })
                .ToListAsync(ct);

            return result;
        }

        private static async Task<bool> ValidateAccountReferenceAsync(Persistence.Context.ShardingSingleDbContext context, int? accountId, CancellationToken ct)
        {
            if (!accountId.HasValue || accountId.Value <= 0)
                return true;

            return await context.Accounts.AsNoTracking().AnyAsync(x => x.Id == accountId.Value, ct);
        }
    }
}
