using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Services.CalculationItems.Tender
{
    public sealed class TenderAttributeCommandService(ShardingSingleDbContext db) : ITenderAttributeCommandService
    {

        // -------------------------------------------------------
        // Create Attribute + bind initial values
        // -------------------------------------------------------
        public async Task<int> CreateAttributeAsync(
            TenderAttributeListPostDTO dto,
            int calculationId,
            CancellationToken ct = default)
        {
            var attr = new TenderAttributeDefinitionEntity(
                calculationId: calculationId,
                name: dto.Name,
                note: dto.Note
            );

            db.AttributeNameTender.Add(attr);
            await db.SaveChangesAsync(ct);

            if (dto.TendersValues is not null)
            {
                foreach (var x in dto.TendersValues)
                {
                    db.TenderAttributeBind.Add(
                        new TenderAttributeBindEntity(
                            tenderId: x.Key,
                            attributeId: attr.Id,
                            value: x.Value));
                }

                await db.SaveChangesAsync(ct);
            }

            return attr.Id;
        }

        // -------------------------------------------------------
        // Update Attribute (only definition)
        // -------------------------------------------------------
        public async Task<bool> UpdateAttributeAsync(
            int id,
            int calculationId,
            TenderAttributePostDTO dto,
            CancellationToken ct = default)
        {
            var attr = await db.AttributeNameTender
                .FirstOrDefaultAsync(x => x.Id == id && x.CalculationId == calculationId, ct);

            if (attr == null)
                return false;

            attr.Update(dto.Name, dto.Note);

            await db.SaveChangesAsync(ct);
            return true;
        }

        // -------------------------------------------------------
        // Delete Attribute + all bound values
        // -------------------------------------------------------
        public async Task<bool> DeleteAttributeAsync(
            int id,
            int calculationId,
            CancellationToken ct = default)
        {
            var binds = await db.TenderAttributeBind
                .Where(x => x.TenderAttributeId == id)
                .ToListAsync(ct);

            if (binds.Count > 0)
            {
                db.TenderAttributeBind.RemoveRange(binds);
                await db.SaveChangesAsync(ct);
            }

            var attr = await db.AttributeNameTender
                .FirstOrDefaultAsync(x => x.Id == id && x.CalculationId == calculationId, ct);

            if (attr == null)
                return false;

            db.AttributeNameTender.Remove(attr);
            await db.SaveChangesAsync(ct);

            return true;
        }
    }
}
