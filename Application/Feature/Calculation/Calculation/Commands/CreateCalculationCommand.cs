using Application.Interfaces;
using AutoMapper;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System;

namespace Application.Feature.Calculation.Calculation.Commands
{
    public sealed record CreateCalculationCommand(PostCalculationDTO Dto, Guid ProjectId, int UserId, int? DepartmentId) : IRequest<int>;

    public class CreateCalculationCommandHandler(IShardingSingleDbContext _dataAccess, IMapper _mapper) : IRequestHandler<CreateCalculationCommand, int>
    {
        public async Task<int> Handle(CreateCalculationCommand request, CancellationToken cancellationToken)
        {
            CalculationEntity calculation = _mapper.Map<CalculationEntity>(request.Dto);
            request.CopyPropertiesTo(calculation.Metadata);

            calculation.ProjectId = request.ProjectId;
            double? max = _dataAccess.Calculations.Where(x => x.ProjectId == request.ProjectId).Max(x => (double?)x.SortOrder);
            if (max.HasValue) calculation.SortOrder = max.Value + 100;
            else calculation.SortOrder = 100;

            calculation.CreatedBy = request.UserId;
            calculation.CreatedAt = DateTime.Now;
            _dataAccess.Calculations.Add(calculation);
            await _dataAccess.SaveChangesAsync(cancellationToken);
            return calculation.Id;
        }
    }
}
