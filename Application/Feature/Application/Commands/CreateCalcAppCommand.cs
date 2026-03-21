using Application.Interfaces;
using ProjectManagement.Shared.DTO.App;

namespace Application.Feature.Application.Commands
{
    public sealed record CreateCalcAppCommand(ApplicationValuesDTO Dto) : IRequest<int>;

    public class CreateCalcAppCommandHandler(IApplicationService context) : IRequestHandler<CreateCalcAppCommand, int>
    {
        public async Task<int> Handle(CreateCalcAppCommand request, CancellationToken cancellationToken)
         => await context.CreateCalcApp(request.Dto, cancellationToken);
    }
}
