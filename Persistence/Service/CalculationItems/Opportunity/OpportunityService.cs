using Application.Interfaces;
using Application.Services.CalculationItems.Opportunity;
using Domain.Entities.Calculation;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems.Opportunity
{
    public sealed class OpportunityService(IDbContextFactoryTenant dbFactory, INotificationHub notification) : IOpportunityService
    {

        public async Task<List<OpportunityEntity>> GetByCalculationAsync(int calculationId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Opportunity
                .Where(x => x.CalculationId == calculationId)
                .AsNoTracking()
                .ToListAsync(ct);
        }
        // -------------------------------------------------
        // CREATE
        // -------------------------------------------------
        public async Task<int> CreateAsync(PostOpportunityDTO dto, int calculationId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var entity = new OpportunityEntity(
                dto.OpportunitiesRisks,
                dto.Type,
                calculationId,
                dto.Data ?? new OpportunityData()
            );

            context.Opportunity.Add(entity);
            await context.SaveChangesAsync(ct);

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
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var entity = await context.Opportunity
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity == null)
                return false;

            entity.Update(
                dto.OpportunitiesRisks,
                dto.Type,
                dto.Data ?? new OpportunityData()
            );

            await context.SaveChangesAsync(ct);

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
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var opp = await context.Opportunity.FindAsync(new object?[] { id }, cancellationToken: ct);
            if (opp == null)
                return false;

            // فك الربط مع المهام والموارد مثل الكود القديم 
            var tasks = context.Tasks.Where(x => x.OpportunityId == opp.Id);
            var res = context.Resources.Where(x => x.OpportunityId == opp.Id);

            foreach (var t in tasks)
                t.OpportunityId = null;

            foreach (var r in res)
                r.OpportunityId = null;

            context.Opportunity.Remove(opp);
            await context.SaveChangesAsync(ct);

            await notification.SendNotificationAsync(
                opp.CalculationId.ToString(),
                ObjectTypHub.Opportunity,
                OperationType.Remove,
                opp.Id);

            return true;
        }
    }

}
