#nullable enable

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
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // Transaction لضمان الذرّية (خصوصاً لأننا قد نحتاج SaveChanges مرتين بسبب Identity Id)
            await using var tx = await context.Database.BeginTransactionAsync(ct);

            var tender = new TenderEntity(
                calculationId: calculationId,
                organisationId: companyId,
                note: dto.Note ?? string.Empty
            );

            context.Tenders.Add(tender);
            await context.SaveChangesAsync(ct); // للحصول على tender.Id

            if (dto.AttributesValue is not null && dto.AttributesValue.Count > 0)
            {
                foreach (var x in dto.AttributesValue)
                {
                    context.TenderAttributeBind.Add(
                        new TenderAttributeBindEntity(
                            tenderId: tender.Id,
                            attributeId: x.Key,
                            value: (decimal)x.Value));
                }

                await context.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
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

            if (tender is null)
                return false;

            tender.UpdateNote(dto.Note ?? string.Empty);
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

            var tender = await context.Tenders
                .FirstOrDefaultAsync(x => x.Id == id && x.CalculationId == calculationId, ct);

            if (tender is null)
                return false;

            // حذف binds + tender في SaveChanges واحد (بدون SaveChanges وسط العملية)
            var binds = await context.TenderAttributeBind
                .Where(x => x.TenderId == id)
                .ToListAsync(ct);

            if (binds.Count > 0)
                context.TenderAttributeBind.RemoveRange(binds);

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
                    x => x.TenderId == tenderId && x.TenderAttributeId == attributeId,
                    ct);

            if (bind is null)
            {
                context.TenderAttributeBind.Add(
                    new TenderAttributeBindEntity(
                        tenderId: tenderId,
                        attributeId: attributeId,
                        value: (decimal)value));
            }
            else
            {
                bind.SetValue((decimal)value);
            }

            await context.SaveChangesAsync(ct);
            return true;
        }
    }
}
