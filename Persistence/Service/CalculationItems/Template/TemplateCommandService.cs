using Application.Mapping.Calculation;
using Application.Services.CalculationItems.TemplateTable;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Factory;
using ProjectManagement.Shared.Constants;
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
                .FirstOrDefaultAsync(
                    x => x.Id == id &&
                         (departmentId.HasValue
                             ? x.DepartmentId == departmentId.Value
                             : !x.DepartmentId.HasValue),
                    ct);

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
                .AnyAsync(
                    x => x.Id == id &&
                         (departmentId.HasValue
                             ? x.DepartmentId == departmentId.Value
                             : !x.DepartmentId.HasValue),
                    ct);

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

        public async Task<TemplateModelDTO?> CopyAsync(int id, int? sourceDepartmentId, int? targetDepartmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // Source can be a common template (no department) or one in the given department.
            var source = await context.Templates
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == id &&
                         (sourceDepartmentId.HasValue
                             ? (x.DepartmentId == sourceDepartmentId.Value || !x.DepartmentId.HasValue)
                             : !x.DepartmentId.HasValue),
                    ct);

            if (source is null)
                return null;

            if (targetDepartmentId.HasValue &&
                !await context.Department.AsNoTracking().AnyAsync(x => x.Id == targetDepartmentId.Value, ct))
                return null;

            var copy = new TemplateEntity($"Kopia av {DisplayStandardName(source.Name)}", true, targetDepartmentId);
            copy.UpdateMetadata(source.GetMetadataSnapshot());

            context.Templates.Add(copy);
            await context.SaveChangesAsync(ct);

            return copy.ToModel();
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

        public async Task<bool> SetScopeDefaultAsync(int id, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var target = await context.Templates
                .FirstOrDefaultAsync(
                    x => x.Id == id &&
                         (departmentId.HasValue
                             ? x.DepartmentId == departmentId.Value
                             : !x.DepartmentId.HasValue),
                    ct);

            if (target is null)
                return false;

            // Only one standardval per scope: clear the flag on the current default(s) in the same scope.
            var siblings = await context.Templates
                .Where(x => x.IsDefault && x.Id != id &&
                            (departmentId.HasValue
                                ? x.DepartmentId == departmentId.Value
                                : !x.DepartmentId.HasValue))
                .ToListAsync(ct);

            foreach (var sibling in siblings)
                sibling.SetDefault(false);

            target.SetDefault(true);
            await context.SaveChangesAsync(ct);
            return true;
        }

        private static string DisplayStandardName(string name)
            => name.Trim() switch
            {
                "Mall01" => "Standard ljus",
                "Mall02" => "Standard mörk",
                "Mall03" => "Utskrift / PDF",
                _ => name
            };
    }
}
