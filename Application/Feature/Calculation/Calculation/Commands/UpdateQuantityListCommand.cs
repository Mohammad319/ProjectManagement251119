using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Calculation.Commands
{
    public sealed record UpdateQuantityListCommand(List<QuanityListDTO> model, int Id, int? DepartmentId) : IRequest<bool>;

    public class UpdateQuantityListCommandHandler(IShardingSingleDbContext _dataAccess, INotificationHub notification) : IRequestHandler<UpdateQuantityListCommand, bool>
    {
        public async Task<bool> Handle(UpdateQuantityListCommand request, CancellationToken cancellationToken)
        {
            CalculationEntity calculation = await _dataAccess.Calculations.FirstOrDefaultAsync(x =>
           x.Id == request.Id && (request.DepartmentId == null || x.Project.Folder.DepartmentId == request.DepartmentId), cancellationToken: cancellationToken);
            if (calculation == null) return false;

            calculation.Metadata.QuanityList = request.model;

            _dataAccess.Calculations.Update(calculation);
            await _dataAccess.SaveChangesAsync(cancellationToken);

            CalculationPageDTO calc = new();
            calculation.CopyPropertiesTo(calc);
            calc.QuanityList = calculation.Metadata.QuanityList;

            await notification.SendNotificationAsync(calculation.Id.ToString(), ObjectTypHub.calculation, OperationType.Update, calc);

            return true;
        }
    }
}