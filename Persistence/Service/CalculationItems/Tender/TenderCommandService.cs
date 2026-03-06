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

        private static async Task SyncAttributeValuesAsync(
            ShardingSingleDbContext context,
            int tenderId,
            Dictionary<int, double> attributes,
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
    }
}
