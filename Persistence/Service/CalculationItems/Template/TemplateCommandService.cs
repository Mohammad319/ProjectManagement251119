using Microsoft.EntityFrameworkCore;
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

            // تجنب CopyPropertiesTo مرتين
            var model = new TemplateModelDTO { Name = template.Name };
            meta.CopyPropertiesTo(model);
            return model;
        }

        // ------------------------------------------------------
        // UPDATE (مضمون: نحافظ على قواعد الدومين)
        // ------------------------------------------------------
        public async Task<bool> UpdateAsync(int id, TemplateListPostDTO dto, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // تحميل كيان واحد فقط (Tracking) ثم تحديث عبر الدومين
            var template = await context.Templates
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (template == null)
                return false;

            var newMeta = new TemplateData();
            dto.CopyPropertiesTo(newMeta);

            template.Update(dto.Name, dto.Active, departmentId, newMeta);

            await context.SaveChangesAsync(ct);
            return true;
        }

        // ------------------------------------------------------
        // DELETE (مضمون + سريع)
        // ------------------------------------------------------
        public async Task<bool> DeleteAsync(int id, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // تحقق وجود فقط (بدون تحميل entity)
            var exists = await context.Templates.AnyAsync(x => x.Id == id, ct);
            if (!exists)
                return false;

            // 1) فك ارتباط الحسابات في SQL مباشرة (عمود بسيط -> آمن)
            await context.Calculations
                .Where(x => x.TemplateId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TemplateId, (int?)null), ct);

            // 2) حذف القالب مباشرة (بدون تحميل)
            await context.Templates
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync(ct);

            return true;
        }

        // ------------------------------------------------------
        // SET DEFAULT TEMPLATE (مضمون + أقل تحميل)
        // ------------------------------------------------------
        public async Task<TemplateModelDTO?> SetDefaultAsync(int calculationId, int? templateId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // تحميل calc لأنك تستخدم SetTemplate (دومين)
            var calc = await context.Calculations
                .FirstOrDefaultAsync(x => x.Id == calculationId, ct);

            if (calc == null)
                return null;

            TemplateEntity? template = null;

            if (templateId is > 0)
            {
                // هنا نحتاج template فقط لو سنعيد metadata
                template = await context.Templates
                    .AsNoTracking() // لا نحتاج tracking
                    .FirstOrDefaultAsync(x => x.Id == templateId, ct);

                if (template == null)
                    return null;
            }

            calc.SetTemplate(templateId is > 0 ? templateId : null);
            await context.SaveChangesAsync(ct);

            var model = new TemplateModelDTO { Name = calc.Name };

            if (template != null)
                template.Metadata.CopyPropertiesTo(model);

            return model;
        }
    }
}
