using Application.Interfaces;
using AutoMapper;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Calculation.Commands
{
    public sealed record CreateOpportunityCommand(PostOpportunityDTO dto, int CalculationId) : IRequest<int>;

        public class CreateOpportunityCommandHandler(IShardingSingleDbContext _dataAccess, IMapper _mapper, INotificationHub notification) : IRequestHandler<CreateOpportunityCommand, int>
        {
            public async Task<int> Handle(CreateOpportunityCommand request, CancellationToken cancellationToken)
            {
                OpportunityEntity Opportunity = _mapper.Map<OpportunityEntity>(request.dto);
                Opportunity.Metadata = request.dto.Data;
                _dataAccess.Opportunity.Add(Opportunity);
            Opportunity.CalculationId = request.CalculationId;
            await _dataAccess.SaveChangesAsync(cancellationToken);
                await notification.SendNotificationAsync(request.CalculationId.ToString(), ObjectTypHub.Opportunity, OperationType.Add, Opportunity);
                return Opportunity.Id;
            }
        }
    }
