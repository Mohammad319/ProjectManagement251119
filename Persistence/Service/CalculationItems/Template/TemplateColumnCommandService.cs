using Application.Mapping.Calculation;
using Application.Services.CalculationItems.TemplateTable;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Persistence.Service.CalculationItems.Template
{
    public sealed class TemplateColumnCommandService(IDbContextFactoryTenant dbFactory) : ITemplateColumnCommandService
    {
        public async Task<TemplateColumnModelDTO> CreateAsync(TemplateColumnPostDTO dto, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (departmentId.HasValue && !await context.Department.AsNoTracking().AnyAsync(x => x.Id == departmentId.Value, ct))
                return new TemplateColumnModelDTO();

            var templateColumn = new TemplateColumnEntity(dto.Name, dto.Active, departmentId, dto.ToColumns());
            context.TemplateColumns.Add(templateColumn);
            await context.SaveChangesAsync(ct);

            return templateColumn.ToModel();
        }

        public async Task<bool> UpdateAsync(int id, TemplateColumnPostDTO dto, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (departmentId.HasValue && !await context.Department.AsNoTracking().AnyAsync(x => x.Id == departmentId.Value, ct))
                return false;

            var templateColumn = await context.TemplateColumns
                .FirstOrDefaultAsync(
                    x => x.Id == id &&
                         (departmentId.HasValue
                             ? x.DepartmentId == departmentId.Value
                             : !x.DepartmentId.HasValue),
                    ct);

            if (templateColumn is null)
                return false;

            templateColumn.Update(dto.Name, dto.Active, departmentId, dto.ToColumns());
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var templateColumnExists = await context.TemplateColumns
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == id &&
                         (departmentId.HasValue
                             ? x.DepartmentId == departmentId.Value
                             : !x.DepartmentId.HasValue),
                    ct);

            if (!templateColumnExists)
                return false;

            await context.Calculations
                .Where(x => x.TemplateColumnId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TemplateColumnId, (int?)null), ct);

            await context.TemplateColumns
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync(ct);

            return true;
        }

        public async Task<TemplateColumnModelDTO?> CopyAsync(int id, int? sourceDepartmentId, int? targetDepartmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var source = await context.TemplateColumns
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

            var copy = new TemplateColumnEntity($"Kopia av {source.Name}", true, targetDepartmentId, source.GetColumnsSnapshot());
            context.TemplateColumns.Add(copy);
            await context.SaveChangesAsync(ct);

            return copy.ToModel();
        }

        public async Task<TemplateColumnModelDTO?> SetDefaultAsync(int calculationId, int? templateColumnId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var calc = await context.Calculations
                .FirstOrDefaultAsync(x => x.Id == calculationId && (!departmentId.HasValue || x.DepartmentId == departmentId.Value), ct);

            if (calc is null)
                return null;

            TemplateColumnEntity? templateColumn = null;

            if (templateColumnId is > 0)
            {
                templateColumn = await context.TemplateColumns
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Id == templateColumnId && (!departmentId.HasValue || x.DepartmentId == null || x.DepartmentId == departmentId.Value),
                        ct);

                if (templateColumn is null)
                    return null;
            }

            calc.SetTemplateColumn(templateColumnId is > 0 ? templateColumnId : null);
            await context.SaveChangesAsync(ct);

            return templateColumn?.ToModel();
        }
    }
}
