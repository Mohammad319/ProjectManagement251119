using Application.Feature.Application;
using Domain.Entities.Application;
using Persistence.Factory;
using ProjectManagement.Shared.Base.Application;

namespace Persistence.Service.Application
{
    public class ApplicationService(IDbContextFactoryTenant dbFactory) : IApplicationService
    {
        public async Task<IEnumerable<ApplicationValuesEntity>> GetCalcAppAsync(int CalcId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.ApplicationValues
                .Include(y => y.Application)
                .Where(x => x.CalculationId == CalcId)
                .OrderByDescending(x => x)
                .ToListAsync(ct);
        }
        public async Task<List<ApplicationEntity>> GetApplicationQueryAsync(bool WithNoneVisible, CancellationToken ct)
        {
                await using var context = await dbFactory.CreateDbContextAsync(ct);
            if (WithNoneVisible)
            {
                return await context.Applications
                    .OrderByDescending(x => x)
                    .Where(x => x.IsVisible == true)
                    .ToListAsync(ct);
            }

            return await context.Applications
                .OrderByDescending(x => x)
                .ToListAsync(ct);
        }
        public async Task<int> CreateAsync(ApplicationEntity Dto, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            ApplicationEntity template = new()
            {
                DepartmentId = Dto.DepartmentId,
                IsVisible = Dto.IsVisible,
                //Description = Dto.Description,
                LastUpdate = DateTime.Now,
                Name = Dto.Name,
                UserId = Dto.UserId,
                Data = Dto.Data,
                //DataStr = JsonSerializer.Serialize(Dto.Rows),
            };

            context.Applications.Add(template);
            await context.SaveChangesAsync(ct);
            return template.Id;
        }
        public async Task<int> CreateCalcApp(ApplicationValuesBase Dto, int CalculationId, int ApplicationId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            ApplicationValuesEntity template = new()
            {
                LastUpdate = DateTime.Now,
                UserId = Dto.UserId,
                ApplicationId = ApplicationId,
                CalculationId = CalculationId,
                Data = Dto.Data,
                Responsible = Dto.Responsible,
                Name = Dto.Name,
            };

            context.ApplicationValues.Add(template);
            await context.SaveChangesAsync();
            return template.Id;
        }
        public async Task<bool> DeleteApplecationAsync(int Id, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var _folder = await context.Applications.FindAsync(Id);
            if (_folder != null)
            {
                context.Applications.Remove(_folder);
                await context.SaveChangesAsync(ct);
                return true;
            }

            return false;
        }
        public async Task<bool> DeleteCalcAppAsync(int Id, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var _folder = await context.ApplicationValues.FindAsync(Id);
            if (_folder != null)
            {
                context.ApplicationValues.Remove(_folder);
                await context.SaveChangesAsync(ct);
                return true;
            }

            return false;
        }
        public async Task<bool> UpdateAsync(ApplicationEntity dto, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var app = await context.Applications.FindAsync(dto.Id);
            if (app == null)
                return false;
            app.LastUpdate = DateTime.Now;
            app.IsVisible = dto.IsVisible;
            app.UserId = dto.UserId;
            app.Name = dto.Name;
            app.Data = dto.Data;

            await context.SaveChangesAsync(ct);
            return true;
        }
        public async Task<bool> UpdateCalcAppAsync(ApplicationValuesEntity dto, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var app = await context.ApplicationValues.FindAsync(new object?[] { dto.Id }, cancellationToken: ct);
            if (app == null)
                return false;
            app.LastUpdate = DateTime.Now;
            app.Data = dto.Data;
            app.UserId = dto.UserId;
            app.Responsible = dto.Responsible;
            app.Name = dto.Name;
            await context.SaveChangesAsync(ct);
            return true;
        }
    }
}
