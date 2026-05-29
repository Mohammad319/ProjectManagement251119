using Application.Interfaces;
using Application.Mapping.Calculation;
using Application.Services.CalculationItems.Opportunity;
using Domain.Entities.Calculation;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems.Opportunity
{
    public sealed class OpportunityService(IDbContextFactoryTenant dbFactory, INotificationHub notification) : IOpportunityService
    {
        public async Task<List<OpportunityListDTO>> GetByCalculationAsync(int calculationId, int? departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var entities = await context.Opportunity
                .Where(x => x.CalculationId == calculationId &&
                    (!departmentId.HasValue || x.Calculation.DepartmentId == departmentId.Value))
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .ToListAsync(ct);
            return entities.Select(x => x.ToListDto()).ToList();
        }

        public async Task<int> CreateAsync(PostOpportunityDTO dto, int calculationId, int? departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var calculationExists = await context.Calculations
                .AsNoTracking()
                .AnyAsync(x => x.Id == calculationId &&
                    !x.IsLocked &&
                    (!departmentId.HasValue || x.DepartmentId == departmentId.Value), ct);

            if (!calculationExists)
                return 0;

            var entity = new OpportunityEntity(
                dto.OpportunitiesRisks,
                dto.OpportunityType,
                calculationId,
                dto.ToMetadata());

            context.Opportunity.Add(entity);
            await context.SaveChangesAsync(ct);

            await notification.SendNotificationAsync(
                calculationId.ToString(),
                ObjectTypHub.Opportunity,
                OperationType.Add,
                entity.ToListDto());

            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, PostOpportunityDTO dto, int? departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var entity = await context.Opportunity
                .FirstOrDefaultAsync(x => x.Id == id &&
                    !x.Calculation.IsLocked &&
                    (!departmentId.HasValue || x.Calculation.DepartmentId == departmentId.Value), ct);

            if (entity is null)
                return false;

            entity.Update(
                dto.OpportunitiesRisks,
                dto.OpportunityType,
                dto.ToMetadata());

            await context.SaveChangesAsync(ct);

            await notification.SendNotificationAsync(
                entity.CalculationId.ToString(),
                ObjectTypHub.Opportunity,
                OperationType.Update,
                entity.ToListDto());

            return true;
        }

        public async Task<bool> DeleteAsync(int id, int? departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var opp = await context.Opportunity
                .FirstOrDefaultAsync(x => x.Id == id &&
                    !x.Calculation.IsLocked &&
                    (!departmentId.HasValue || x.Calculation.DepartmentId == departmentId.Value), ct);
            if (opp is null)
                return false;

            var tasks = await context.Tasks
                .Where(x => x.OpportunityId == opp.Id && x.CalculationId == opp.CalculationId)
                .ToListAsync(ct);

            var taskIds = tasks.Select(t => t.Id).ToHashSet();
            var resources = await context.Resources
                .Where(x => x.OpportunityId == opp.Id && taskIds.Contains(x.TaskId))
                .ToListAsync(ct);

            foreach (var task in tasks)
                task.SetOpportunity(null);

            foreach (var resource in resources)
                resource.SetOpportunity(null);

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
