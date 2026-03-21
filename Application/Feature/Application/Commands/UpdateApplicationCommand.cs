using Application.Interfaces;
using ProjectManagement.Shared.DTO.App;

namespace Application.Feature.Application.Commands
{
    public sealed record UpdateApplicationCommand(ApplicationDTO dto) : IRequest<bool>;

    public class UpdateApplicationCommandHandler(IApplicationService dataAccess) : IRequestHandler<UpdateApplicationCommand, bool>
    {
        public async Task<bool> Handle(UpdateApplicationCommand request, CancellationToken cancellationToken)
         => await dataAccess.UpdateAsync(request.dto, cancellationToken);
    }
}
