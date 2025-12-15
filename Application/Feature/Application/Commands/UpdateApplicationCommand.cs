using Application.Interfaces;
using Domain.Entities.Application;
using System;

namespace Application.Feature.Application.Commands
{
    public sealed record UpdateApplicationCommand(ApplicationEntity dto) : IRequest<bool>;

    public class UpdateApplicationCommandHandler(IApplicationService _dataAccess) : IRequestHandler<UpdateApplicationCommand, bool>
    {
        public async Task<bool> Handle(UpdateApplicationCommand request, CancellationToken cancellationToken)
         => await _dataAccess.UpdateAsync(request.dto, cancellationToken);

    }
}
