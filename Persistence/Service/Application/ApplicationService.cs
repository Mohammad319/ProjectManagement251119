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
                .Include(x => x.Application)
                .Where(x => x.CalculationId == calcId)
                .OrderByDescending(x => x.LastUpdate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);
        }

        public async Task<List<ApplicationEntity>> GetApplicationQueryAsync(bool withNoneVisible, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var query = context.Applications.AsQueryable();
            if (!withNoneVisible)
                query = query.Where(x => x.IsVisible);

            return await query
                .OrderByDescending(x => x.LastUpdate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);
        }

        public async Task<int> CreateAsync(ApplicationEntity dto, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

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

        public async Task<bool> DeleteApplecationAsync(int id, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.Applications.FindAsync([id], cancellationToken: ct);
            if (entity is null)
                return false;

            context.Applications.Remove(entity);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteCalcAppAsync(int id, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.ApplicationValues.FindAsync([id], cancellationToken: ct);
            if (entity is null)
                return false;

            context.ApplicationValues.Remove(entity);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateAsync(ApplicationEntity dto, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.Applications.FindAsync([dto.Id], cancellationToken: ct);
            if (entity is null)
                return false;

            entity.UpdateFrom(dto);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateCalcAppAsync(ApplicationValuesEntity dto, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.ApplicationValues.FindAsync([dto.Id], cancellationToken: ct);
            if (entity is null)
                return false;

            entity.UpdateFrom(dto);
            await context.SaveChangesAsync(ct);
            return true;
        }
    }
}
