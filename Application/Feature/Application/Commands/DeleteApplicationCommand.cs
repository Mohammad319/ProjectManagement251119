using Application.Interfaces;

namespace Application.Feature.Application.Commands
{
    public sealed record DeleteApplicationCommand(int Id) : IRequest<bool>;

    public class DeleteApplicationCommandHandler(IApplicationService service) : IRequestHandler<DeleteApplicationCommand, bool>
    {
        public Task<bool> Handle(DeleteApplicationCommand request, CancellationToken cancellationToken)
            => service.DeleteApplicationAsync(request.Id, cancellationToken);
    }
}
