using Application.Interfaces;
using AutoMapper;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Calculation.Commands
{
    public sealed record UpdateCalculationCommand(PostCalculationDTO dto, int Id, int UserId, int? DepartmentId) : IRequest<bool>;

    public class UpdateCalculationCommandHandler(IShardingSingleDbContext _dataAccess, IMapper _mapper, INotificationHub notification) : IRequestHandler<UpdateCalculationCommand, bool>
    {
        public async Task<bool> Handle(UpdateCalculationCommand request, CancellationToken cancellationToken)
        {
            var calculation = await _dataAccess.Calculations.FirstOrDefaultAsync(x =>
           x.Id == request.Id && (request.DepartmentId == null || x.Project.Folder.DepartmentId == request.DepartmentId), cancellationToken: cancellationToken);
            if (calculation == null) return false;
            _mapper.Map(request.dto, calculation);
            request.CopyPropertiesTo(calculation.Metadata);
            _dataAccess.Calculations.Update(calculation);
            await _dataAccess.SaveChangesAsync(cancellationToken);

            CalculationPageDTO calc = new();
            calculation.CopyPropertiesTo(calc);
            await notification.SendNotificationAsync(calculation.Id.ToString(), ObjectTypHub.calculation, OperationType.Update, calc);

            return true;
        }
    }
}
