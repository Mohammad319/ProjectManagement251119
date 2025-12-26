using Domain.Entities.Calculation;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Services.CalculationItems.Tender
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
            var attr = new TenderAttributeDefinitionEntity(
                calculationId: calculationId,
                name: dto.Name,
                note: dto.Note
            );
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            context.AttributeNameTender.Add(attr);
            await context.SaveChangesAsync(ct);

            if (dto.TendersValues is not null)
            {
                foreach (var x in dto.TendersValues)
                {
                    context.TenderAttributeBind.Add(
                        new TenderAttributeBindEntity(
                            tenderId: x.Key,
                            attributeId: attr.Id,
                            value: x.Value));
                }

                await context.SaveChangesAsync(ct);
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
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var attr = await context.AttributeNameTender
                .FirstOrDefaultAsync(x => x.Id == id && x.CalculationId == calculationId, ct);

            if (attr == null)
                return false;

            attr.Update(dto.Name, dto.Note);

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

            var binds = await context.TenderAttributeBind
                .Where(x => x.TenderAttributeId == id)
                .ToListAsync(ct);

            if (binds.Count > 0)
            {
                context.TenderAttributeBind.RemoveRange(binds);
                await context.SaveChangesAsync(ct);
            }

            var attr = await context.AttributeNameTender
                .FirstOrDefaultAsync(x => x.Id == id && x.CalculationId == calculationId, ct);

            if (attr == null)
                return false;

            context.AttributeNameTender.Remove(attr);
            await context.SaveChangesAsync(ct);

            return true;
        }
    }
}
