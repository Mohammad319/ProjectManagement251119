#nullable enable

using Application.Services.CalculationItems.Tender;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems.Tender
{
    public sealed class TenderAttributeQueryService(IDbContextFactoryTenant dbFactory) : ITenderAttributeQueryService
    {
        public async Task<List<TenderAttributeListDTO>> GetAttributesAsync(
            int calculationId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.AttributeNameTender
                .Where(x => x.CalculationId == calculationId &&
                    (!departmentId.HasValue || x.Calculation.DepartmentId == departmentId.Value))
                .AsNoTracking()
                .Select(x => new TenderAttributeListDTO
                {
                    Id = x.Id,
                    Name = x.Name ?? string.Empty,
                    Note = x.Note ?? string.Empty
                })
                .ToListAsync(ct);
        }
    }
}
