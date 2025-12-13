using Application.Interfaces;
using Application.Services.CalculationItems.Storage;

namespace Application.Feature.Calculation.Storage.Commands
{
    public sealed record CreateStorageCommand(
    CalculationItemType Type,
    AuthorityStorage Level,
    StorageSort Sort,
    int Id,
    int UserId,
    int? DepartmentId
) : IRequest<bool>;

    public sealed class CreateStorageCommandHandler(IStorageCommandService service)
                : IRequestHandler<CreateStorageCommand, bool>
    {
        public Task<bool> Handle(CreateStorageCommand request, CancellationToken ct)
            => service.CreateAsync(request.Type, request.Level, request.Sort, request.Id, request.UserId, request.DepartmentId, ct);
    }
}
