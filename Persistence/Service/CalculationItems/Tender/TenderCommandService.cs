#nullable enable

using Application.Services.CalculationItems.Tender;
using Domain.Entities.Calculation;
using Persistence.Context;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;
using System.Collections.Generic;
using System.Linq;

namespace Persistence.Service.CalculationItems.Tender
{
    public sealed class TenderCommandService(IDbContextFactoryTenant dbFactory) : ITenderCommandService
    {
        public async Task<int> CreateTenderAsync(
            TenderPostDTO dto,
            int calculationId,
            int companyId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            await using var tx = await context.Database.BeginTransactionAsync(ct);

            if (!await context.Calculations.AsNoTracking().AnyAsync(x => x.Id == calculationId, ct))
                return 0;

            if (!await context.Organisation.AsNoTracking().AnyAsync(x => x.Id == companyId, ct))
                return 0;

            if (!await AreAttributeIdsValidAsync(context, calculationId, dto.AttributesValue?.Keys, ct))
                return 0;

            var tender = new TenderEntity(
                calculationId: calculationId,
                organisationId: companyId,
                note: dto.Note);

            context.Tenders.Add(tender);
            await context.SaveChangesAsync(ct);

            await SyncAttributeValuesAsync(context, tender.Id, dto.AttributesValue, ct);
            await tx.CommitAsync(ct);
            return tender.Id;
        }

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

            if (!await AreAttributeIdsValidAsync(context, calculationId, dto.AttributesValue?.Keys, ct))
                return false;

            tender.UpdateNote(dto.Note);

            if (dto.AttributesValue is not null)
                await SyncAttributeValuesAsync(context, tender.Id, dto.AttributesValue, ct);
            else
                await context.SaveChangesAsync(ct);

            return true;
        }

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

            var binds = await context.TenderAttributeBind
                .Where(x => x.TenderId == id)
                .ToListAsync(ct);

            if (binds.Count > 0)
                context.TenderAttributeBind.RemoveRange(binds);

            context.Tenders.Remove(tender);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateTenderAttributeValueAsync(
            int tenderId,
            int attributeId,
            double value,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var tenderInfo = await context.Tenders
                .AsNoTracking()
                .Where(x => x.Id == tenderId)
                .Select(x => new { x.Id, x.CalculationId })
                .FirstOrDefaultAsync(ct);

            if (tenderInfo is null)
                return false;

            var attributeExists = await context.AttributeNameTender
                .AsNoTracking()
                .AnyAsync(x => x.Id == attributeId && x.CalculationId == tenderInfo.CalculationId, ct);

            if (!attributeExists)
                return false;

            var bind = await context.TenderAttributeBind
                .FirstOrDefaultAsync(x => x.TenderId == tenderId && x.TenderAttributeId == attributeId, ct);

            if (bind is null)
            {
                context.TenderAttributeBind.Add(new TenderAttributeBindEntity(tenderId, attributeId, value));
            }
            else
            {
                bind.SetValue(value);
            }

            await context.SaveChangesAsync(ct);
            return true;
        }

        private static async global::System.Threading.Tasks.Task SyncAttributeValuesAsync(
            ShardingSingleDbContext context,
            int tenderId,
            Dictionary<int, double>? attributes,
            CancellationToken ct)
        {
            attributes ??= [];

            var existing = await context.TenderAttributeBind
                .Where(x => x.TenderId == tenderId)
                .ToListAsync(ct);

            var incomingIds = attributes.Keys.ToHashSet();
            var toRemove = existing.Where(x => !incomingIds.Contains(x.TenderAttributeId)).ToList();
            if (toRemove.Count > 0)
                context.TenderAttributeBind.RemoveRange(toRemove);

            foreach (var pair in attributes)
            {
                var bind = existing.FirstOrDefault(x => x.TenderAttributeId == pair.Key);
                if (bind is null)
                {
                    context.TenderAttributeBind.Add(new TenderAttributeBindEntity(tenderId, pair.Key, pair.Value));
                }
                else
                {
                    bind.SetValue(pair.Value);
                }
            }

            await context.SaveChangesAsync(ct);
        }

        private static async Task<bool> AreAttributeIdsValidAsync(
            ShardingSingleDbContext context,
            int calculationId,
            IEnumerable<int>? attributeIds,
            CancellationToken ct)
        {
            var ids = attributeIds?
                .Where(x => x > 0)
                .Distinct()
                .ToArray();

            if (ids is null || ids.Length == 0)
                return true;

            var validCount = await context.AttributeNameTender
                .AsNoTracking()
                .Where(x => x.CalculationId == calculationId && ids.Contains(x.Id))
                .CountAsync(ct);

            return validCount == ids.Length;
        }
    }
}
