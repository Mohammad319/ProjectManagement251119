using Application.Interfaces;
using ProjectManagement.Shared.DTO.App;

namespace Application.Feature.Application.Commands
{
    public sealed record UpdateCalcAppCommand(ApplicationValuesDTO dto) : IRequest<bool>;

    public class UpdateCalcAppCommandHandler(IApplicationService dataAccess)
        : IRequestHandler<UpdateCalcAppCommand, bool>
    {
        public async Task<bool> Handle(UpdateCalcAppCommand request, CancellationToken cancellationToken)
         => await dataAccess.UpdateCalcAppAsync(request.dto, cancellationToken);
    }
}
