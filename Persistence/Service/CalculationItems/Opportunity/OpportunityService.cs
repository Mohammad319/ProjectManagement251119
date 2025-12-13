using Application.Interfaces;
using Application.Services.CalculationItems.Opportunity;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;

namespace Persistence.Service.CalculationItems.Opportunity
{
    public sealed class OpportunityService(ShardingSingleDbContext db, INotificationHub notification) : IOpportunityService
    {

        public Task<List<OpportunityEntity>> GetByCalculationAsync(int calculationId, CancellationToken ct = default)
        {
            return db.Opportunity
                .Where(x => x.CalculationId == calculationId)
                .AsNoTracking()
                .ToListAsync(ct);
        }
        // -------------------------------------------------
        // CREATE
        // -------------------------------------------------
        public async Task<int> CreateAsync(PostOpportunityDTO dto, int calculationId, CancellationToken ct = default)
        {
            var entity = new OpportunityEntity(
                dto.OpportunitiesRisks,
                dto.Type,
                calculationId,
                dto.Data ?? new OpportunityData()
            );

            db.Opportunity.Add(entity);
            await db.SaveChangesAsync(ct);

            await notification.SendNotificationAsync(
                calculationId.ToString(),
                ObjectTypHub.Opportunity,
                OperationType.Add,
                entity);

            return entity.Id;
        }

        // -------------------------------------------------
        // UPDATE
        // -------------------------------------------------
        public async Task<bool> UpdateAsync(int id, PostOpportunityDTO dto, CancellationToken ct = default)
        {
            var entity = await db.Opportunity
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity == null)
                return false;

            entity.Update(
                dto.OpportunitiesRisks,
                dto.Type,
                dto.Data ?? new OpportunityData()
            );

            await db.SaveChangesAsync(ct);

            var snapshot = entity.CreateSnapshot();

            await notification.SendNotificationAsync(
                entity.CalculationId.ToString(),
                ObjectTypHub.Opportunity,
                OperationType.Update,
                snapshot);

            return true;
        }

        // -------------------------------------------------
        // DELETE
        // -------------------------------------------------
        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var opp = await db.Opportunity.FindAsync(id);
            if (opp == null)
                return false;

            // فك الربط مع المهام والموارد مثل الكود القديم 
            var tasks = db.Tasks.Where(x => x.OpportunityId == opp.Id);
            var res = db.Resources.Where(x => x.OpportunityId == opp.Id);

            foreach (var t in tasks)
                t.OpportunityId = null;

            foreach (var r in res)
                r.OpportunityId = null;

            db.Opportunity.Remove(opp);
            await db.SaveChangesAsync(ct);

            await notification.SendNotificationAsync(
                opp.CalculationId.ToString(),
                ObjectTypHub.Opportunity,
                OperationType.Remove,
                opp.Id);

            return true;
        }
    }

}
