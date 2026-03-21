using Application.Mapping.Calculation;
using Application.Services.CalculationItems.TemplateTable;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Persistence.Service.CalculationItems.Template
{
    public sealed class TemplateCommandService(IDbContextFactoryTenant dbFactory) : ITemplateCommandService
    {
        public async Task<TemplateModelDTO> CreateAsync(TemplateListPostDTO dto, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (departmentId.HasValue && !await context.Department.AsNoTracking().AnyAsync(x => x.Id == departmentId.Value, ct))
                return new TemplateModelDTO();

            var template = new TemplateEntity(dto.Name, dto.Active, departmentId);
            template.UpdateMetadata(dto.ToTemplateData());

            context.Templates.Add(template);
            await context.SaveChangesAsync(ct);

            return template.ToModel();
        }

        public async Task<bool> UpdateAsync(int id, TemplateListPostDTO dto, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (departmentId.HasValue && !await context.Department.AsNoTracking().AnyAsync(x => x.Id == departmentId.Value, ct))
                return false;

            var template = await context.Templates
                .FirstOrDefaultAsync(x => x.Id == id && (!departmentId.HasValue || x.DepartmentId == departmentId.Value), ct);

            if (template == null)
                return false;

            template.Update(dto.Name, dto.Active, departmentId, dto.ToTemplateData());

            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var templateExists = await context.Templates
                .AsNoTracking()
                .AnyAsync(x => x.Id == id && (!departmentId.HasValue || x.DepartmentId == departmentId.Value), ct);

            if (!templateExists)
                return false;

            await context.Calculations
                .Where(x => x.TemplateId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TemplateId, (int?)null), ct);

            await context.Templates
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync(ct);

            return true;
        }

        public async Task<TemplateModelDTO?> SetDefaultAsync(int calculationId, int? templateId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var calc = await context.Calculations
                .FirstOrDefaultAsync(x => x.Id == calculationId && (!departmentId.HasValue || x.DepartmentId == departmentId.Value), ct);

            if (calc == null)
                return null;

            TemplateEntity? template = null;

            if (templateId is > 0)
            {
                template = await context.Templates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Id == templateId && (!departmentId.HasValue || x.DepartmentId == null || x.DepartmentId == departmentId.Value),
                        ct);

                if (template == null)
                    return null;
            }

            calc.SetTemplate(templateId is > 0 ? templateId : null);
            await context.SaveChangesAsync(ct);

            return template != null
                ? template.ToModel()
                : new TemplateModelDTO { Id = 0, Name = calc.Name };
        }
    }
}
