using Application.Services.CalculationItems.Tender;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems.Tender
{
    public sealed class TenderCommandService(ShardingSingleDbContext db) : ITenderCommandService
    {

        // -------------------------------------------------------
        // Create Tender
        // -------------------------------------------------------
        public async Task<int> CreateTenderAsync(
            TenderPostDTO dto,
            int calculationId,
            int companyId,
            CancellationToken ct = default)
        {
            var tender = new TenderEntity(
                calculationId: calculationId,
                organisationId: companyId,
                note: dto.Note
            );

            db.Tenders.Add(tender);
            await db.SaveChangesAsync(ct);

            // Bind attributes
            if (dto.AttributesValue is not null)
            {
                foreach (var x in dto.AttributesValue)
                {
                    db.TenderAttributeBind.Add(
                        new TenderAttributeBindEntity(
                            tenderId: tender.Id,
                            attributeId: x.Key,
                            value: x.Value));
                }

                await db.SaveChangesAsync(ct);
            }

            return tender.Id;
        }

        // -------------------------------------------------------
        // Update Tender
        // -------------------------------------------------------
        public async Task<bool> UpdateTenderAsync(
            int id,
            int calculationId,
            TenderPostDTO dto,
            CancellationToken ct = default)
        {
            var tender = await db.Tenders
                .FirstOrDefaultAsync(x => x.Id == id && x.CalculationId == calculationId, ct);

            if (tender == null)
                return false;

            tender.UpdateNote(dto.Note);

            await db.SaveChangesAsync(ct);

            return true;
        }

        // -------------------------------------------------------
        // Delete Tender
        // -------------------------------------------------------
        public async Task<bool> DeleteTenderAsync(
            int id,
            int calculationId,
            CancellationToken ct = default)
        {
            // حذف قيم attributes
            var binds = await db.TenderAttributeBind
                .Where(x => x.TenderId == id)
                .ToListAsync(ct);

            if (binds.Count > 0)
            {
                db.TenderAttributeBind.RemoveRange(binds);
                await db.SaveChangesAsync(ct);
            }

            // حذف العطاء نفسه
            var tender = await db.Tenders
                .FirstOrDefaultAsync(x => x.Id == id && x.CalculationId == calculationId, ct);

            if (tender == null)
                return false;

            db.Tenders.Remove(tender);
            await db.SaveChangesAsync(ct);

            return true;
        }

        // -------------------------------------------------------
        // Update Attribute Value for Tender
        // -------------------------------------------------------
        public async Task<bool> UpdateTenderAttributeValueAsync(
            int tenderId,
            int attributeId,
            double value,
            CancellationToken ct = default)
        {
            var bind = await db.TenderAttributeBind
                .FirstOrDefaultAsync(
                    x => x.TenderId == tenderId &&
                         x.TenderAttributeId == attributeId,
                    ct);

            if (bind == null)
            {
                bind = new TenderAttributeBindEntity(
                    tenderId: tenderId,
                    attributeId: attributeId,
                    value: value);

                db.TenderAttributeBind.Add(bind);
            }
            else
            {
                bind.SetValue(value);
            }

            await db.SaveChangesAsync(ct);
            return true;
        }
    }
}
