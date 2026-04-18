using Application.Mapping.Calculation;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Application.Services.CalculationItems.TemplateTable
{
    public sealed class TemplateQueryService(IDbContextFactoryTenant dbFactory) : ITemplateQueryService
    {
        public async Task<TemplateModelDTO?> GetByIdAsync(
            int id,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var template = await context.Templates
                .AsNoTracking()
                .Where(x => x.Id == id &&
                            (!x.DepartmentId.HasValue || x.DepartmentId == departmentId || !departmentId.HasValue))
                .FirstOrDefaultAsync(ct);

            return template?.ToModel();
        }

        public async Task<List<TemplateListDTO>> GetByUserAsync(
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.Templates
                .AsNoTracking()
                .Where(x => !departmentId.HasValue || x.DepartmentId == departmentId || !x.DepartmentId.HasValue)
                .OrderByDescending(x => x.Id)
                .Select(x => new TemplateListDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    DepartmentId = x.DepartmentId
                })
                .ToListAsync(ct);
        }
    }
}
