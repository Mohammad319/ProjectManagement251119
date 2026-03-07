using Application.Interfaces;

namespace Application.Feature.Application.Commands
{
    public sealed record DeleteCalcAppCommand(int Id) : IRequest<bool>;

    public class DeleteCalcAppCommandHandler(IApplicationService service) : IRequestHandler<DeleteCalcAppCommand, bool>
    {
        public Task<bool> Handle(DeleteCalcAppCommand request, CancellationToken cancellationToken)
            => service.DeleteCalcAppAsync(request.Id, cancellationToken);
    }
}
