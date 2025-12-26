using Application.Feature.ResourceType;
using Persistence.Context;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Persistence.Service.ResourceType
{
    public sealed class ResourceTypeService(IDbContextFactoryTenant dbFactory) : IResourceTypeService
    {

        // ---------------- Commands (Type) ----------------

        public async Task<int> CreateTypeAsync(PostResourceTypeDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var max = await context.ResourceTypes.MaxAsync(x => (int?)x.SortOrder, ct) ?? 0;
            var entity = Domain.Entities.ResourceType.ResourceTypeEntity.Create(dto, max + 100);

            context.ResourceTypes.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> UpdateTypeAsync(int id, PostResourceTypeDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.ResourceTypes.FindAsync(id, ct);
            if (entity == null) return false;

            entity.Update(dto);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteTypeAsync(int id, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.ResourceTypes.FindAsync(id, ct);
            if (entity == null) return false;

            context.ResourceTypes.Remove(entity);
            await context.SaveChangesAsync(ct);
            return true;
        }

        // ---------------- Commands (Sort) ----------------

        public async Task<int> CreateSortAsync(int resourceTypeId, PostResourceSortDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            bool typeExists = await context.ResourceTypes.AnyAsync(x => x.Id == resourceTypeId, ct);
            if (!typeExists) return 0;

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

            var entity = await context.ResourceSorts.FindAsync(id, ct);
            if (entity == null) return false;

            entity.Update(dto);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteSortAsync(int id, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.ResourceSorts.FindAsync(id, ct);
            if (entity == null) return false;

            context.ResourceSorts.Remove(entity);
            await context.SaveChangesAsync(ct);
            return true;
        }

        // ---------------- Queries ----------------

        public async Task<List<ResourceTypeModel>> GetTypesAsync(bool isVisible, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.ResourceTypes
                .AsNoTracking()
                .Where(x => x.IsVisible == isVisible)
                .OrderBy(x => x.SortOrder)
                .Select(x => new ResourceTypeModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Type = x.Kind,
                    IsVisible = x.IsVisible,
                    Order = x.SortOrder,
                    AccountId = x.AccountId,

                    // metadata
                    Cost = x.Metadata.Cost,
                    BaseCost = x.Metadata.BaseCost,
                    Unit = x.Metadata.Unit,
                    FixedQ = x.Metadata.FixedQ,
                    CO2 = x.Metadata.CO2,
                    CapWaste = x.Metadata.CapWaste,
                    ChangeFactor1 = x.Metadata.ChangeFactor1,
                    ChangeFactor2 = x.Metadata.ChangeFactor2
                })
                .ToListAsync(ct);
        }

        public async Task<List<ResourceSortModel>> GetSortsAsync(int resourceTypeId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.ResourceSorts
                .AsNoTracking()
                .Where(x => x.ResourceTypeId == resourceTypeId)
                .OrderBy(x => x.SortOrder)
                .Select(x => new ResourceSortModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsVisible = x.IsVisible,
                    Order = x.SortOrder,
                    ResourceTypeId = x.ResourceTypeId,
                    AccountId = x.AccountId,

                    // metadata
                    Cost = x.Metadata.Cost,
                    BaseCost = x.Metadata.BaseCost,
                    Unit = x.Metadata.Unit,
                    FixedQ = x.Metadata.FixedQ,
                    CO2 = x.Metadata.CO2,
                    CapWaste = x.Metadata.CapWaste,
                    ChangeFactor1 = x.Metadata.ChangeFactor1,
                    ChangeFactor2 = x.Metadata.ChangeFactor2
                })
                .ToListAsync(ct);
        }

        public async Task<ResourceFormDTO> GetVisualFormAsync(CancellationToken ct = default)
        {
            // بديل نظيف لـ GetVisualResourcesQuery القديم :contentReference[oaicite:3]{index=3}
            var result = new ResourceFormDTO();
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            result.ResourceTypes = await context.ResourceTypes
                .AsNoTracking()
                .Where(x => x.IsVisible)
                .OrderBy(x => x.SortOrder)
                .Select(x => new ListResourceTypeDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Type = x.Kind,
                    Order = x.SortOrder,
                    IsVisible = x.IsVisible,
                    AccountId = x.AccountId ?? 0,

                    ChangeFactor1 = x.Metadata.ChangeFactor1,
                    ChangeFactor2 = x.Metadata.ChangeFactor2,
                    BaseCost = x.Metadata.BaseCost,
                    CapWaste = x.Metadata.CapWaste,
                    CO2 = x.Metadata.CO2,
                    Cost = x.Metadata.Cost,
                    FixedQ = x.Metadata.FixedQ,
                    Unit = x.Metadata.Unit,

                    ResourcesSort = x.ResourcesSort
                        .OrderBy(rs => rs.SortOrder)
                        .Select(rs => new ListResourceSortDTO
                        {
                            Id = rs.Id,
                            Name = rs.Name,
                            Order = rs.SortOrder,
                            IsVisible = rs.IsVisible,
                            AccountId = rs.AccountId ?? 0,

                            ChangeFactor1 = rs.Metadata.ChangeFactor1,
                            ChangeFactor2 = rs.Metadata.ChangeFactor2,
                            BaseCost = rs.Metadata.BaseCost,
                            CapWaste = rs.Metadata.CapWaste,
                            CO2 = rs.Metadata.CO2,
                            Cost = rs.Metadata.Cost,
                            FixedQ = rs.Metadata.FixedQ,
                            Unit = rs.Metadata.Unit
                        })
                        .ToList()
                })
                .ToListAsync(ct);

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
    }
}
