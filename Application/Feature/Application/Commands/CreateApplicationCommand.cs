using Application.Interfaces;
using ProjectManagement.Shared.DTO.App;

namespace Application.Feature.Application.Commands
{
    public sealed record CreateApplicationCommand(ApplicationDTO Dto) : IRequest<int>;

    public class CreateApplicationCommandHandler(IApplicationService context) : IRequestHandler<CreateApplicationCommand, int>
    {
        public async Task<int> Handle(CreateApplicationCommand request, CancellationToken cancellationToken)
         => await context.CreateAsync(request.Dto, cancellationToken);
    }
}
