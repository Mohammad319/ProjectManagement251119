using Application.Helper;
using Application.Services.CalculationItems.TemplateTable;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Persistence.Service.CalculationItems.Template
{
    public sealed class TemplateCommandService(ShardingSingleDbContext db) : ITemplateCommandService
    {

        // ------------------------------------------------------
        // CREATE
        // ------------------------------------------------------
        public async Task<TemplateModelDTO> CreateAsync(TemplateListPostDTO dto, int? departmentId, CancellationToken ct)
        {
            var template = new TemplateEntity(dto.Name, dto.Active, departmentId);
            var meta = new TemplateData();
            dto.CopyPropertiesTo(meta);
            template.UpdateMetadata(meta);

            db.Templates.Add(template);
            await db.SaveChangesAsync(ct);

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
            var template = await db.Templates.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (template == null)
                return false;

            TemplateData newMeta = new();
            dto.CopyPropertiesTo(newMeta);

            template.Update(dto.Name, dto.Active, departmentId, newMeta);

            await db.SaveChangesAsync(ct);
            return true;
        }

        // ------------------------------------------------------
        // DELETE
        // ------------------------------------------------------
        public async Task<bool> DeleteAsync(int id, int? departmentId, CancellationToken ct)
        {
            var template = await db.Templates.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (template == null)
                return false;

            // فك ارتباط الحسابات
            var calcs = await db.Calculations.Where(x => x.TemplateId == id).ToListAsync(ct);
            foreach (var calc in calcs)
                calc.SetTemplate(null);

            db.Calculations.UpdateRange(calcs);
            db.Templates.Remove(template);

            await db.SaveChangesAsync(ct);
            return true;
        }

        // ------------------------------------------------------
        // SET DEFAULT TEMPLATE
        // ------------------------------------------------------
        public async Task<TemplateModelDTO?> SetDefaultAsync(int calculationId, int? templateId, int? departmentId, CancellationToken ct)
        {
            var calc = await db.Calculations.FirstOrDefaultAsync(x => x.Id == calculationId, ct);
            if (calc == null)
                return null;

            TemplateEntity? template = null;

            if (templateId.HasValue && templateId > 0)
            {
                template = await db.Templates.FirstOrDefaultAsync(x => x.Id == templateId, ct);
                if (template == null)
                    return null;
            }

            calc.SetTemplate(templateId);
            await db.SaveChangesAsync(ct);

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
