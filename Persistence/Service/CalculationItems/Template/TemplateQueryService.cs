using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Application.Services.CalculationItems.TemplateTable
{
    public sealed class TemplateQueryService(IDbContextFactoryTenant dbFactory) : ITemplateQueryService
    {

        // مكافئ GetTemplateByIdQuery القديم :contentReference[oaicite:1]{index=1}
        public async Task<TemplateModelDTO?> GetByIdAsync(
            int id,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Templates
                .AsNoTracking()
                .Where(x => x.Id == id &&
                            (!departmentId.HasValue || x.DepartmentId == departmentId))
                .Select(x => new TemplateModelDTO
                {
                    Name = x.Name,
                    Currency = x.Metadata.Currency,
                    DateFormat = x.Metadata.DateFormat,
                    FreezList = x.Metadata.FreezList,
                    MathRound = x.Metadata.MathRound,
                    NetColor = x.Metadata.NetColor,
                    NetOrder = x.Metadata.NetOrder,
                    NetWidth = x.Metadata.NetWidth,
                    SSColor = x.Metadata.SSColor,
                    SSOrder = x.Metadata.SSOrder,
                    SSWidth = x.Metadata.SSWidth,
                })
                .FirstOrDefaultAsync(ct);
        }

        // مكافئ GetTemplatesByUserQuery القديم :contentReference[oaicite:2]{index=2}
        public async Task<List<TemplateListDTO>> GetByUserAsync(
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Templates
                .AsNoTracking()
                .Where(x => x.DepartmentId == departmentId)
                .OrderByDescending(x => x.Id) // إصلاح OrderByDescending(x => x)
                .Select(x => new TemplateListDTO
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync(ct);
        }
    }
}
