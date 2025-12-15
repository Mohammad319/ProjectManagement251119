using Application.Interfaces;

namespace Application.Feature.Application.Commands
{
    public sealed record DeleteApplicationCommand(int Id) : IRequest<bool>;

    public class DeleteApplicationCommandHandler(IApplicationService _context) : IRequestHandler<DeleteApplicationCommand, bool>
    {
        public async Task<bool> Handle(DeleteApplicationCommand request, CancellationToken cancellationToken)
         => await _context.DeleteApplecationAsync(request.Id, cancellationToken);

    }
}
