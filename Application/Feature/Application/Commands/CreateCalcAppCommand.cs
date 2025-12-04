using Application.Interfaces;
using Domain.Entities.Application;
using ProjectManagement.Shared.Base.Application;
using System;

namespace Application.Feature.Application.Commands
{
    public sealed record CreateCalcAppCommand(ApplicationValuesBase Dto, int CalculationId, int ApplicationId) : IRequest<int>;

    public class CreateCalcAppCommandHandler(IShardingSingleDbContext context) : IRequestHandler<CreateCalcAppCommand, int>
    {
        public async Task<int> Handle(CreateCalcAppCommand request, CancellationToken cancellationToken)
        {
            ApplicationValuesEntity template = new()
            {
                LastUpdate = DateTime.Now,
                UserId = request.Dto.UserId,
                ApplicationId = request.ApplicationId,
                CalculationId = request.CalculationId,
                Data = request.Dto.Data,
                Responsible = request.Dto.Responsible,
                Name = request.Dto.Name,
            };

            context.ApplicationValues.Add(template);
            await context.SaveChangesAsync();
            return template.Id;
        }
    }
}
