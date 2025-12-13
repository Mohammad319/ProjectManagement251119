using Application.Interfaces;
using Application.Services.CalculationItems.Storage;

namespace Application.Feature.Calculation.Storage.Commands
{
    public sealed record DeleteStorageCommand(int Id) : IRequest<bool>;

    public sealed class DeleteStorageCommandHandler(IStorageCommandService service)
                : IRequestHandler<DeleteStorageCommand, bool>
    {
        public Task<bool> Handle(DeleteStorageCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }

}
