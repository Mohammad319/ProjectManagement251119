using Application.Services.CalculationItems.Tender;
using Domain.Entities.Calculation;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems.Tender
{
    public sealed class TenderCommandService(IDbContextFactoryTenant dbFactory) : ITenderCommandService
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
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            context.Tenders.Add(tender);
            await context.SaveChangesAsync(ct);

            // Bind attributes
            if (dto.AttributesValue is not null)
            {
                foreach (var x in dto.AttributesValue)
                {
                    context.TenderAttributeBind.Add(
                        new TenderAttributeBindEntity(
                            tenderId: tender.Id,
                            attributeId: x.Key,
                            value: x.Value));
                }

                await context.SaveChangesAsync(ct);
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
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var tender = await context.Tenders
                .FirstOrDefaultAsync(x => x.Id == id && x.CalculationId == calculationId, ct);

            if (tender == null)
                return false;

            tender.UpdateNote(dto.Note);

            await context.SaveChangesAsync(ct);

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
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // حذف قيم attributes
            var binds = await context.TenderAttributeBind
                .Where(x => x.TenderId == id)
                .ToListAsync(ct);

            if (binds.Count > 0)
            {
                context.TenderAttributeBind.RemoveRange(binds);
                await context.SaveChangesAsync(ct);
            }

            // حذف العطاء نفسه
            var tender = await context.Tenders
                .FirstOrDefaultAsync(x => x.Id == id && x.CalculationId == calculationId, ct);

            if (tender == null)
                return false;

            context.Tenders.Remove(tender);
            await context.SaveChangesAsync(ct);

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
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var bind = await context.TenderAttributeBind
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

                context.TenderAttributeBind.Add(bind);
            }
            else
            {
                bind.SetValue(value);
            }

            await context.SaveChangesAsync(ct);
            return true;
        }
    }
}
