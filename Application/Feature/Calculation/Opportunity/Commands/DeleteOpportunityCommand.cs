using Application.Interfaces;

namespace Application.Feature.Calculation.Calculation.Commands
{
    public sealed record DeleteOpportunityCommand(int Id) : IRequest<bool>;

    public class DeleteOpportunityCommandHandler(IShardingSingleDbContext _dataAccess, INotificationHub notification) : IRequestHandler<DeleteOpportunityCommand, bool>
    {
        public async Task<bool> Handle(DeleteOpportunityCommand request, CancellationToken cancellationToken)
        {
            var Opportunity = await _dataAccess.Opportunity.FindAsync(request.Id);
            if (Opportunity == null)
                return false;
            var tasks = _dataAccess.Tasks.Where(x => x.OpportunityId == Opportunity.Id);
            var res = _dataAccess.Resources.Where(x => x.OpportunityId == Opportunity.Id);
            if (tasks != null) foreach (var task in tasks) task.OpportunityId = null;
            if (res != null) foreach (var task in res) task.OpportunityId = null;

            _dataAccess.Opportunity.Remove(Opportunity);
            await _dataAccess.SaveChangesAsync();
            await notification.SendNotificationAsync(Opportunity.CalculationId.ToString(), ObjectTypHub.Opportunity, OperationType.Remove, Opportunity.Id);

            return true;
        }
    }
}
