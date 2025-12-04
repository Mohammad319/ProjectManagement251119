using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;
using System;

namespace Application.Feature.Calculation.Calculation.Commands
{
    public sealed record HourlyPriceListCommand(int Id, List<HourlyPriceListGroupDTO> HourlyPriceList, int UserId, int? DepartmentId) : IRequest<bool>;

    public class HourlyPriceListCommandHandler(IShardingSingleDbContext _dataAccess, INotificationHub notification) : IRequestHandler<HourlyPriceListCommand, bool>
    {
        public async Task<bool> Handle(HourlyPriceListCommand request, CancellationToken cancellationToken)
        {
            var calculation = await _dataAccess.Calculation.FirstOrDefaultAsync(x => x.Id == request.Id
            && (!request.DepartmentId.HasValue || x.Project.Folder.DepartmentId == request.DepartmentId));
            if (calculation == null)
                return false;

            calculation.HourlyPriceFactorData.HourlyPrice = request.HourlyPriceList;

            _dataAccess.Calculation.Update(calculation);
            await _dataAccess.SaveChangesAsync();

            await notification.SendNotificationAsync(calculation.Id.ToString(),
                ObjectTypHub.HourlyPrice, OperationType.Update, request.HourlyPriceList);

            return true;
        }
    }
}
