using Application.Interfaces;

namespace Application.Feature.Calculation.CalcShare.Commands
{
    public sealed record DeleteCalcShareCommand(int Id, int DepartmentId, int UserId) : IRequest<bool>;
    public class DeleteCalcShareCommandHandler(IShardingSingleDbContext _dataAccess) : IRequestHandler<DeleteCalcShareCommand, bool>
    {
        public async Task<bool> Handle(DeleteCalcShareCommand request, CancellationToken cancellationToken)
        {
            var calculation = await _dataAccess.ShareCalc.FindAsync(request.Id);
            if (calculation == null)
                return false;
            _dataAccess.ShareCalc.Remove(calculation);
            await _dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
