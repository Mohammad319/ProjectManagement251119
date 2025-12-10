using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System;

namespace Application.Feature.Calculation.Calculation.Commands
{
    public sealed record UpdateFactorsCommand(List<OHFactors> model, int Id, int? DepartmentId) : IRequest<bool>;

        public class UpdateFactorsCommandHandler(IShardingSingleDbContext _dataAccess, INotificationHub notification) : IRequestHandler<UpdateFactorsCommand, bool>
        {
            public async Task<bool> Handle(UpdateFactorsCommand request, CancellationToken cancellationToken)
            {
                CalculationEntity calculation = await _dataAccess.Calculations.FirstOrDefaultAsync(x =>
               x.Id == request.Id && (request.DepartmentId == null || x.Project.Folder.DepartmentId == request.DepartmentId), cancellationToken: cancellationToken);
                if (calculation == null) return false;

                calculation.HourlyPriceFactorData.Factors = request.model;
                _dataAccess.Calculations.Update(calculation);
                await _dataAccess.SaveChangesAsync(cancellationToken);

                CalculationPageDTO calc = new();
                calculation.CopyPropertiesTo(calc);
                calc.Factors = calculation.HourlyPriceFactorData.Factors;

                await notification.SendNotificationAsync(calculation.Id.ToString(), ObjectTypHub.calculation, OperationType.Update, calc);

                return true;
            }
        }
    }