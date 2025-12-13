using Application.Interfaces;
using Application.Services.CalculationItems.Storage;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Calculation.Storage.Queries;

public sealed record GetStorageQuery(
    CalculationItemType Type,
    AuthorityStorage Level,
    StorageSort Sort
) : IRequest<IEnumerable<StorageDTO<object>>>;

public sealed class GetStorageQueryHandler(IStorageQueryService service)
        : IRequestHandler<GetStorageQuery, IEnumerable<StorageDTO<object>>>
{
    public Task<IEnumerable<StorageDTO<object>>> Handle(GetStorageQuery request, CancellationToken ct)
        => service.GetAsync(request.Type, request.Level, request.Sort, ct);
}
