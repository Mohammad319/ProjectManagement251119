using Application.Interfaces.Context;
using Application.Services.CalculationItems.Tender;
using ProjectManagement.Shared.DTO.Calculation;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Service.CalculationItems.Tender
{
    public sealed class TenderAttributeQueryService(IShardingSingleDbContext db) : ITenderAttributeQueryService
    {
        public async Task<List<TenderAttributeListDTO>> GetAttributesAsync(
            int calculationId,
            CancellationToken ct = default)
        {
            return await db.AttributeNameTender
                .Where(x => x.CalculationId == calculationId)
                .Select(x => new TenderAttributeListDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Note = x.Note
                })
                .ToListAsync(ct);
        }
    }

}
