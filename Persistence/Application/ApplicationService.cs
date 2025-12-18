using Application.Feature.Application;
using Domain.Entities.Application;
using ProjectManagement.Shared.Base.Application;

namespace Persistence.Application
{
    public class ApplicationService(ShardingSingleDbContext context) : IApplicationService
    {
        public async Task<IEnumerable<ApplicationValuesEntity>> GetCalcAppAsync(int CalcId, CancellationToken cancellationToken)
        {
            return await context.ApplicationValues
                .Include(y => y.Application)
                .Where(x => x.CalculationId == CalcId)
                .OrderByDescending(x => x)
                .ToListAsync(cancellationToken);
        }
        public async Task<List<ApplicationEntity>> GetApplicationQueryAsync(bool WithNoneVisible, CancellationToken cancellationToken)
        {
            if (WithNoneVisible)
            {
                return await context.Applications
                    .OrderByDescending(x => x)
                    .Where(x => x.IsVisible == true)
                    .ToListAsync(cancellationToken);
            }

            return await context.Applications
                .OrderByDescending(x => x)
                .ToListAsync(cancellationToken);
        }
        public async Task<int> CreateAsync(ApplicationEntity Dto, CancellationToken cancellationToken)
        {
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
            await context.SaveChangesAsync(cancellationToken);
            return template.Id;
        }
        public async Task<int> CreateCalcApp(ApplicationValuesBase Dto, int CalculationId, int ApplicationId, CancellationToken cancellationToken)
        {
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
        public async Task<bool> DeleteApplecationAsync(int Id, CancellationToken cancellationToken)
        {
            var _folder = await context.Applications.FindAsync(Id);
            if (_folder != null)
            {
                context.Applications.Remove(_folder);
                await context.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false;
        }
        public async Task<bool> DeleteCalcAppAsync(int Id, CancellationToken cancellationToken)
        {
            var _folder = await context.ApplicationValues.FindAsync(Id);
            if (_folder != null)
            {
                context.ApplicationValues.Remove(_folder);
                await context.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false;
        }
        public async Task<bool> UpdateAsync(ApplicationEntity dto, CancellationToken cancellationToken)
        {
            var app = await context.Applications.FindAsync(dto.Id);
            if (app == null)
                return false;
            app.LastUpdate = DateTime.Now;
            app.IsVisible = dto.IsVisible;
            app.UserId = dto.UserId;
            app.Name = dto.Name;
            app.Data = dto.Data;

            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        public async Task<bool> UpdateCalcAppAsync(ApplicationValuesEntity dto, CancellationToken cancellationToken)
        {
            var app = await context.ApplicationValues.FindAsync(dto.Id);
            if (app == null)
                return false;
            app.LastUpdate = DateTime.Now;
            app.Data = dto.Data;
            app.UserId = dto.UserId;
            app.Responsible = dto.Responsible;
            app.Name = dto.Name;
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
