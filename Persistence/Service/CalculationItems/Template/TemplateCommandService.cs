using Application.Services.CalculationItems.TemplateTable;
using Domain.Entities.Calculation;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Persistence.Service.CalculationItems.Template
{
    public sealed class TemplateCommandService(IDbContextFactoryTenant dbFactory) : ITemplateCommandService
    {

        // ------------------------------------------------------
        // CREATE
        // ------------------------------------------------------
        public async Task<TemplateModelDTO> CreateAsync(TemplateListPostDTO dto, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var template = new TemplateEntity(dto.Name, dto.Active, departmentId);
            var meta = new TemplateData();
            dto.CopyPropertiesTo(meta);
            template.UpdateMetadata(meta);

            context.Templates.Add(template);
            await context.SaveChangesAsync(ct);

            TemplateModelDTO model = new()
            {
                Name = template.Name
            };

            template.Metadata.CopyPropertiesTo(model);
            return model;
        }

        // ------------------------------------------------------
        // UPDATE
        // ------------------------------------------------------
        public async Task<bool> UpdateAsync(int id, TemplateListPostDTO dto, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var template = await context.Templates.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (template == null)
                return false;

            TemplateData newMeta = new();
            dto.CopyPropertiesTo(newMeta);

            template.Update(dto.Name, dto.Active, departmentId, newMeta);

            await context.SaveChangesAsync(ct);
            return true;
        }

        // ------------------------------------------------------
        // DELETE
        // ------------------------------------------------------
        public async Task<bool> DeleteAsync(int id, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var template = await context.Templates.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (template == null)
                return false;

            // فك ارتباط الحسابات
            var calcs = await context.Calculations.Where(x => x.TemplateId == id).ToListAsync(ct);
            foreach (var calc in calcs)
                calc.SetTemplate(null);

            context.Calculations.UpdateRange(calcs);
            context.Templates.Remove(template);

            await context.SaveChangesAsync(ct);
            return true;
        }

        // ------------------------------------------------------
        // SET DEFAULT TEMPLATE
        // ------------------------------------------------------
        public async Task<TemplateModelDTO?> SetDefaultAsync(int calculationId, int? templateId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var calc = await context.Calculations.FirstOrDefaultAsync(x => x.Id == calculationId, ct);
            if (calc == null)
                return null;

            TemplateEntity? template = null;

            if (templateId.HasValue && templateId > 0)
            {
                template = await context.Templates.FirstOrDefaultAsync(x => x.Id == templateId, ct);
                if (template == null)
                    return null;
            }

            calc.SetTemplate(templateId);
            await context.SaveChangesAsync(ct);

            TemplateModelDTO model = new()
            {
                Name = calc.Name
            };

            if (template != null)
                template.Metadata.CopyPropertiesTo(model);

            return model;
        }
    }

}
