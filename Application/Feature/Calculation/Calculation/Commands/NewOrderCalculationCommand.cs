using Application.Interfaces;

namespace Application.Feature.Calculation.Calculation.Commands
{
    public sealed record NewOrderCalculationCommand(int Id, double NewOrder) : IRequest<bool>;

    public class NewOrderCalculationCommandHandler(IShardingSingleDbContext _dataAccess) : IRequestHandler<NewOrderCalculationCommand, bool>
    {
        public async Task<bool> Handle(NewOrderCalculationCommand request, CancellationToken cancellationToken)
        {
            var calc = await _dataAccess.Calculations.FindAsync(request.Id);
            if (calc == null)
                return false;

            calc.SortOrder = request.NewOrder;
            await _dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
