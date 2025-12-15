using Application.Interfaces;

namespace Application.Feature.Application.Commands
{
    public sealed record DeleteCalcAppCommand(int Id) : IRequest<bool>;

    public class DeleteCalcAppCommandHandler(IApplicationService _dataAccess) : IRequestHandler<DeleteCalcAppCommand, bool>
    {
        public async Task<bool> Handle(DeleteCalcAppCommand request, CancellationToken cancellationToken)
         => await _dataAccess.DeleteApplecationAsync(request.Id, cancellationToken);

    }
}
