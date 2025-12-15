using Application.Interfaces;
using Domain.Entities.Application;
using System;

namespace Application.Feature.Application.Commands
{
    public sealed record UpdateCalcAppCommand(ApplicationValuesEntity dto) : IRequest<bool>;
    public class UpdateCalcAppCommandHandler(IApplicationService _dataAccess) 
        : IRequestHandler<UpdateCalcAppCommand, bool>
    {
        public async Task<bool> Handle(UpdateCalcAppCommand request, CancellationToken cancellationToken)
         => await _dataAccess.UpdateCalcAppAsync(request.dto, cancellationToken);

    }

}
