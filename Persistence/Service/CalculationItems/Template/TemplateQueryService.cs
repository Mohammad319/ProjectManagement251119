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
                            (departmentId.HasValue
                                ? (!x.DepartmentId.HasValue || x.DepartmentId == departmentId)
                                : !x.DepartmentId.HasValue))
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
                .Where(x => departmentId.HasValue
                    ? x.DepartmentId == departmentId || !x.DepartmentId.HasValue
                    : !x.DepartmentId.HasValue)
                .OrderByDescending(x => x.Id)
                .Select(x => new TemplateListDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    DepartmentId = x.DepartmentId,
                    IsVisible = x.IsVisible,
                    IsDefault = x.IsDefault
                })
                .ToListAsync(ct);
        }

        // Standardval used for a new calculation when it has no own choice: department default first, then
        // the company (no-department) default, then the seeded system default, then none.
        private const string SystemDefaultName = "Standard ljus";

        public async Task<int?> ResolveDefaultIdAsync(int? departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (departmentId.HasValue)
            {
                var deptDefault = await context.Templates
                    .AsNoTracking()
                    .Where(x => x.DepartmentId == departmentId.Value && x.IsDefault && x.IsVisible)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(ct);

                if (deptDefault.HasValue)
                    return deptDefault;
            }

            var companyDefault = await context.Templates
                .AsNoTracking()
                .Where(x => !x.DepartmentId.HasValue && x.IsDefault && x.IsVisible)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(ct);

            if (companyDefault.HasValue)
                return companyDefault;

            return await context.Templates
                .AsNoTracking()
                .Where(x => !x.DepartmentId.HasValue && x.Name == SystemDefaultName)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(ct);
        }
    }
}
