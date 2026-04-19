using Application.Mapping.Calculation;
using Application.Services.CalculationItems.TemplateTable;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Persistence.Service.CalculationItems.Template
{
    public sealed class TemplateColumnQueryService(IDbContextFactoryTenant dbFactory) : ITemplateColumnQueryService
    {
        public async Task<TemplateColumnModelDTO?> GetByIdAsync(
            int id,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var templateColumn = await context.TemplateColumns
                .AsNoTracking()
                .Where(x => x.Id == id &&
                            (departmentId.HasValue
                                ? (!x.DepartmentId.HasValue || x.DepartmentId == departmentId)
                                : !x.DepartmentId.HasValue))
                .FirstOrDefaultAsync(ct);

            return templateColumn?.ToModel();
        }

        public async Task<List<TemplateColumnListDTO>> GetByUserAsync(
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.TemplateColumns
                .AsNoTracking()
                .Where(x => departmentId.HasValue
                    ? x.DepartmentId == departmentId || !x.DepartmentId.HasValue
                    : !x.DepartmentId.HasValue)
                .OrderByDescending(x => x.Id)
                .Select(x => new TemplateColumnListDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    DepartmentId = x.DepartmentId
                })
                .ToListAsync(ct);
        }
    }
}
