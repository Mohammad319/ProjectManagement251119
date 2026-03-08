using Application.Feature.Application;
using Domain.Entities.Application;
using Persistence.Factory;
using ProjectManagement.Shared.Base.Application;

namespace Persistence.Service.Application
{
    public class ApplicationService(IDbContextFactoryTenant dbFactory) : IApplicationService
    {
        public async Task<IEnumerable<ApplicationValuesEntity>> GetCalcAppAsync(int calcId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.ApplicationValues
                .AsNoTracking()
                .Include(x => x.Application)
                .Where(x => x.CalculationId == calcId)
                .OrderByDescending(x => x.LastUpdate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);
        }

        public async Task<List<ApplicationEntity>> GetApplicationQueryAsync(bool withNoneVisible, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var query = context.Applications.AsNoTracking().AsQueryable();
            if (!withNoneVisible)
                query = query.Where(x => x.IsVisible);

            return await query
                .OrderByDescending(x => x.LastUpdate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<ApplicationListItemDto>> GetApplicationListAsync(bool withNoneVisible, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

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

        public async Task<int> CreateAsync(ApplicationEntity dto, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var departmentExists = await context.Department
                .AsNoTracking()
                .AnyAsync(x => x.Id == dto.DepartmentId, ct);

            if (!departmentExists)
                return 0;

            var entity = ApplicationEntity.Create(
                dto.DepartmentId,
                dto.IsVisible,
                dto.UserId,
                dto.Name,
                dto.Data);

            context.Applications.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<int> CreateCalcApp(ApplicationValuesBase dto, int calculationId, int applicationId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var calculationExists = await context.Calculations
                .AsNoTracking()
                .AnyAsync(x => x.Id == calculationId, ct);

            if (!calculationExists)
                return 0;

            var applicationExists = await context.Applications
                .AsNoTracking()
                .AnyAsync(x => x.Id == applicationId, ct);

            if (!applicationExists)
                return 0;

            var existingId = await context.ApplicationValues
                .AsNoTracking()
                .Where(x => x.CalculationId == calculationId && x.ApplicationId == applicationId)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(ct);

            if (existingId.HasValue)
                return existingId.Value;

            var entity = ApplicationValuesEntity.Create(
                calculationId,
                applicationId,
                dto.UserId,
                dto.Name,
                dto.Responsible,
                dto.Data);

            context.ApplicationValues.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity.Id;
        }

        public Task<bool> DeleteApplecationAsync(int id, CancellationToken ct)
            => DeleteApplicationAsync(id, ct);

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

        public async Task<bool> UpdateAsync(ApplicationEntity dto, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.Applications
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (entity is null)
                return false;

            entity.UpdateFrom(dto);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateCalcAppAsync(ApplicationValuesEntity dto, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.ApplicationValues
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (entity is null)
                return false;

            entity.UpdateFrom(dto);
            await context.SaveChangesAsync(ct);
            return true;
        }
    }
}
