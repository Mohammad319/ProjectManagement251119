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
            request.CopyPropertiesTo(calculation.Data);

            calculation.ProjectId = request.ProjectId;
            double? max = _dataAccess.Calculation.Where(x => x.ProjectId == request.ProjectId).Max(x => (double?)x.Order);
            if (max.HasValue) calculation.Order = max.Value + 100;
            else calculation.Order = 100;

            calculation.UserId = request.UserId;
            calculation.Created = DateTime.Now;
            _dataAccess.Calculation.Add(calculation);
            await _dataAccess.SaveChangesAsync(cancellationToken);
            return calculation.Id;
        }
    }
}
