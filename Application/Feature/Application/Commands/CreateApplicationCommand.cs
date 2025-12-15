using Application.Interfaces;
using Domain.Entities.Application;
using ProjectManagement.Shared.Base.Application;
using System;

namespace Application.Feature.Application.Commands
{
    public sealed record CreateApplicationCommand(ApplicationEntity Dto) : IRequest<int>;

    public class CreateApplicationCommandHandler(IApplicationService context) : IRequestHandler<CreateApplicationCommand, int>
    {
        public async Task<int> Handle(CreateApplicationCommand request, CancellationToken cancellationToken)
         => await context.CreateAsync(request.Dto, cancellationToken);
    }
}
