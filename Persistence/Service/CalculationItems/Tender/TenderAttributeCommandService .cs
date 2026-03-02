#nullable enable

using Application;
using Application.Services.CalculationItems.Tender;
using Domain.Entities.Calculation;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems.Tender
{
    public sealed class TenderAttributeCommandService(IDbContextFactoryTenant dbFactory) : ITenderAttributeCommandService
    {
        // -------------------------------------------------------
        // Create Attribute + bind initial values
        // -------------------------------------------------------
        public async Task<int> CreateAttributeAsync(
            TenderAttributeListPostDTO dto,
            int calculationId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            await using var tx = await context.Database.BeginTransactionAsync(ct);

            var attr = new TenderAttributeDefinitionEntity(
                calculationId: calculationId,
                name: dto.Name ?? string.Empty,
                note: dto.Note
            );

            context.AttributeNameTender.Add(attr);
            await context.SaveChangesAsync(ct); // للحصول على attr.Id

            if (dto.TendersValues is not null && dto.TendersValues.Count > 0)
            {
                foreach (var x in dto.TendersValues)
                {
                    context.TenderAttributeBind.Add(
                        new TenderAttributeBindEntity(
                            tenderId: x.Key,
                            attributeId: attr.Id,
                            value: (decimal)x.Value));
                }

                await context.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
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
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var attr = await context.AttributeNameTender
                .FirstOrDefaultAsync(x => x.Id == id && x.CalculationId == calculationId, ct);

            if (attr is null)
                return false;

            attr.Update(dto.Name ?? string.Empty, dto.Note);
            await context.SaveChangesAsync(ct);

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
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var attr = await context.AttributeNameTender
                .FirstOrDefaultAsync(x => x.Id == id && x.CalculationId == calculationId, ct);

            if (attr is null)
                return false;

            // حذف binds + attr في SaveChanges واحد
            var binds = await context.TenderAttributeBind
                .Where(x => x.TenderAttributeId == id)
                .ToListAsync(ct);

            if (binds.Count > 0)
                context.TenderAttributeBind.RemoveRange(binds);

            context.AttributeNameTender.Remove(attr);
            await context.SaveChangesAsync(ct);

            return true;
        }
    }
}
