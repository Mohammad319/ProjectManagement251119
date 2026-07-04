using Application.Feature.Application;
using Application.Mapping.App;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.App;

namespace Persistence.Service.Application
{
    public class ApplicationService(IDbContextFactoryTenant dbFactory) : IApplicationService
    {
        public async Task<List<ApplicationValuesDTO>> GetCalcAppAsync(int calcId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var entities = await context.ApplicationValues
                .AsNoTracking()
                .Include(x => x.Application)
                .Where(x => x.CalculationId == calcId)
                .OrderByDescending(x => x.LastUpdate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);

            return entities.Select(x => x.ToDto()).ToList();
        }

        public async Task<List<ApplicationDTO>> GetApplicationQueryAsync(bool withNoneVisible, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            await EnsureStandardTemplatesAsync(context, ct);

            var query = context.Applications.AsNoTracking().AsQueryable();
            if (!withNoneVisible)
                query = query.Where(x => x.IsVisible);

            var entities = await query
                .OrderByDescending(x => x.LastUpdate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);

            return entities.Select(x => x.ToDto()).ToList();
        }

        public async Task<IReadOnlyList<ApplicationListItemDto>> GetApplicationListAsync(bool withNoneVisible, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            await EnsureStandardTemplatesAsync(context, ct);

            var query = context.Applications.AsNoTracking().AsQueryable();
            if (!withNoneVisible)
                query = query.Where(x => x.IsVisible);

            return await query
                .OrderByDescending(x => x.LastUpdate)
                .ThenByDescending(x => x.Id)
                .Select(x => new ApplicationListItemDto
                {
                    Id = x.Id,
                    DepartmentId = x.DepartmentId,
                    Name = x.Name,
                    IsVisible = x.IsVisible,
                    UserId = x.UserId,
                    LastUpdate = x.LastUpdate,
                    Description = x.Data.Description,
                    RowCount = x.Data.Rows.Count
                })
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<ApplicationValueListItemDto>> GetCalcAppListAsync(int calcId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.ApplicationValues
                .AsNoTracking()
                .Where(x => x.CalculationId == calcId)
                .OrderByDescending(x => x.LastUpdate)
                .ThenByDescending(x => x.Id)
                .Select(x => new ApplicationValueListItemDto
                {
                    Id = x.Id,
                    CalculationId = x.CalculationId,
                    ApplicationId = x.ApplicationId,
                    ApplicationName = x.Application.Name,
                    Name = x.Name,
                    Responsible = x.Responsible,
                    UserId = x.UserId,
                    LastUpdate = x.LastUpdate,
                    AttributeValueCount = x.Data.Attributes.Count
                })
                .ToListAsync(ct);
        }

        public async Task<int> CreateAsync(ApplicationDTO dto, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var departmentExists = await context.Department
                .AsNoTracking()
                .AnyAsync(x => x.Id == dto.DepartmentId, ct);

            if (!departmentExists)
                return 0;

            var payload = dto.ToEntity();
            payload.Data.IsSystemTemplate = false;
            payload.Data.SystemTemplateKey = string.Empty;
            var entity = Domain.Entities.Application.ApplicationEntity.Create(
                payload.DepartmentId,
                payload.IsVisible,
                payload.UserId,
                payload.Name,
                payload.Data);
            context.Applications.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<int> CreateCalcApp(ApplicationValuesDTO dto, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var calculationExists = await context.Calculations
                .AsNoTracking()
                .AnyAsync(x => x.Id == dto.CalculationId, ct);

            if (!calculationExists)
                return 0;

            var applicationExists = await context.Applications
                .AsNoTracking()
                .AnyAsync(x => x.Id == dto.ApplicationId, ct);

            if (!applicationExists)
                return 0;

            var existingId = await context.ApplicationValues
                .AsNoTracking()
                .Where(x => x.CalculationId == dto.CalculationId && x.ApplicationId == dto.ApplicationId)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(ct);

            if (existingId.HasValue)
                return existingId.Value;

            var payload = dto.ToEntity();
            var entity = Domain.Entities.Application.ApplicationValuesEntity.Create(
                payload.CalculationId,
                payload.ApplicationId,
                payload.UserId,
                payload.Name,
                payload.Responsible,
                payload.Data);
            context.ApplicationValues.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> DeleteApplicationAsync(int id, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var isUsed = await context.ApplicationValues
                .AsNoTracking()
                .AnyAsync(x => x.ApplicationId == id, ct);

            if (isUsed)
                return false;

            var entity = await context.Applications
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null)
                return false;
            if (entity.Data.IsSystemTemplate)
                return false;

            context.Applications.Remove(entity);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteCalcAppAsync(int id, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.ApplicationValues
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null)
                return false;

            context.ApplicationValues.Remove(entity);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateAsync(ApplicationDTO dto, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.Applications
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (entity is null)
                return false;
            if (entity.Data.IsSystemTemplate)
                return false;

            entity.UpdateFrom(dto.ToEntity());
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateCalcAppAsync(ApplicationValuesDTO dto, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.ApplicationValues
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (entity is null)
                return false;

            entity.UpdateFrom(dto.ToEntity());
            await context.SaveChangesAsync(ct);
            return true;
        }

        private static async Task EnsureStandardTemplatesAsync(ShardingSingleDbContext context, CancellationToken ct)
        {
            var departmentId = await context.Department
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(ct);

            if (!departmentId.HasValue)
                return;

            var existingKeys = (await context.Applications
                    .AsNoTracking()
                    .ToListAsync(ct))
                .Where(x => x.Data.IsSystemTemplate && !string.IsNullOrWhiteSpace(x.Data.SystemTemplateKey))
                .Select(x => x.Data.SystemTemplateKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missingTemplates = SelfInspectionStandardTemplates
                .CreateSystemTemplates(departmentId.Value)
                .Where(x => !existingKeys.Contains(x.Data.SystemTemplateKey))
                .ToList();

            if (missingTemplates.Count == 0)
                return;

            foreach (var template in missingTemplates)
            {
                var payload = template.ToEntity();
                var entity = Domain.Entities.Application.ApplicationEntity.Create(
                    payload.DepartmentId,
                    payload.IsVisible,
                    payload.UserId,
                    payload.Name,
                    payload.Data);
                context.Applications.Add(entity);
            }

            await context.SaveChangesAsync(ct);
        }
    }
}
