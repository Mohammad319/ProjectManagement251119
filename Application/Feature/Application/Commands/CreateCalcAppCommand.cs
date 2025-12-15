using Application.Interfaces;
using Domain.Entities.Application;
using ProjectManagement.Shared.Base.Application;
using System;
using static ProjectManagement.Shared.Constant.URLConst;

namespace Application.Feature.Application.Commands
{
    public sealed record CreateCalcAppCommand(ApplicationValuesBase Dto, int CalculationId, int ApplicationId) : IRequest<int>;

    public class CreateCalcAppCommandHandler(IApplicationService context) : IRequestHandler<CreateCalcAppCommand, int>
    {
        public async Task<int> Handle(CreateCalcAppCommand request, CancellationToken cancellationToken)
         => await context.CreateCalcApp(request.Dto, request.CalculationId, request.ApplicationId, cancellationToken);
    }
}
