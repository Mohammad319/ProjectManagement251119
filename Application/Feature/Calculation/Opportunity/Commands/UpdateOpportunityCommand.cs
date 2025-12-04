using Application.Interfaces;
using Application.Interfaces.Context;
using AutoMapper;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.DTO.Calculation;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Calculation.Calculation.Commands
{
    public sealed record UpdateOpportunityCommand(PostOpportunityDTO dto,int Id) : IRequest<bool>;

        public class UpdateOpportunityCommandHandler(IShardingSingleDbContext _dataAccess, IMapper _mapper, INotificationHub notification) : IRequestHandler<UpdateOpportunityCommand, bool>
        {
            public async Task<bool> Handle(UpdateOpportunityCommand request, CancellationToken cancellationToken)
            {
                OpportunityEntity Opportunity = await _dataAccess.Opportunity.FirstOrDefaultAsync(x => x.Id == request.Id);
                if (Opportunity == null) return false;
                _mapper.Map(request.dto, Opportunity);
                _dataAccess.Opportunity.Update(Opportunity);
                await _dataAccess.SaveChangesAsync(cancellationToken);
                OpportunityEntity obj = new();
                Opportunity.CopyPropertiesTo(obj);
                await notification.SendNotificationAsync(Opportunity.CalculationId.ToString(),
                    ObjectTypHub.Opportunity, OperationType.Update, obj);
                return true;
            }
        }
    }
