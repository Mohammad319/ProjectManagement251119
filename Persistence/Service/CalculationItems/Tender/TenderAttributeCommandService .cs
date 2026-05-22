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
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            await using var tx = await context.Database.BeginTransactionAsync(ct);

            if (!await IsCalculationAllowedAsync(context, calculationId, departmentId, ct))
                return 0;

            if (!await AreTenderIdsAllowedAsync(context, dto.TendersValues?.Keys, calculationId, departmentId, ct))
                return 0;

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
                            value: x.Value));
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
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var attr = await context.AttributeNameTender
                .FirstOrDefaultAsync(x => x.Id == id &&
                    x.CalculationId == calculationId &&
                    (!departmentId.HasValue || x.Calculation.DepartmentId == departmentId.Value), ct);

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
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var attr = await context.AttributeNameTender
                .FirstOrDefaultAsync(x => x.Id == id &&
                    x.CalculationId == calculationId &&
                    (!departmentId.HasValue || x.Calculation.DepartmentId == departmentId.Value), ct);

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

        private static Task<bool> IsCalculationAllowedAsync(
            Persistence.Context.ShardingSingleDbContext context,
            int calculationId,
            int? departmentId,
            CancellationToken ct)
        {
            return context.Calculations
                .AsNoTracking()
                .AnyAsync(x => x.Id == calculationId &&
                    (!departmentId.HasValue || x.DepartmentId == departmentId.Value), ct);
        }

        private static async Task<bool> AreTenderIdsAllowedAsync(
            Persistence.Context.ShardingSingleDbContext context,
            IEnumerable<int>? tenderIds,
            int calculationId,
            int? departmentId,
            CancellationToken ct)
        {
            var ids = tenderIds?
                .Where(x => x > 0)
                .Distinct()
                .ToArray();

            if (ids is null || ids.Length == 0)
                return true;

            var validCount = await context.Tenders
                .AsNoTracking()
                .Where(x => ids.Contains(x.Id) &&
                    x.CalculationId == calculationId &&
                    (!departmentId.HasValue || x.Calculation.DepartmentId == departmentId.Value))
                .CountAsync(ct);

            return validCount == ids.Length;
        }
    }
}
