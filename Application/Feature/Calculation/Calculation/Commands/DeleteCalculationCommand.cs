using Application.Interfaces;

namespace Application.Feature.Calculation.Calculation.Commands
{
    public sealed record DeleteCalculationCommand(int Id, int UserId, int? DepartmentId) : IRequest<bool>;
    public class DeleteCalculationCommandHandler(IShardingSingleDbContext _dataAccess) : IRequestHandler<DeleteCalculationCommand, bool>
    {
        public async Task<bool> Handle(DeleteCalculationCommand request, CancellationToken cancellationToken)
        {
            var calculation = await _dataAccess.Calculations.FirstOrDefaultAsync(x =>
           x.Id == request.Id && (request.DepartmentId == null || x.Project.Folder.DepartmentId == request.DepartmentId), cancellationToken: cancellationToken);
            if (calculation == null)
                return false;

            _dataAccess.Calculations.Remove(calculation);
            await _dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
