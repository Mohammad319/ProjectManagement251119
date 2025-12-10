using System.Text.Json;
using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Calculation.Storage.Queries;

public sealed record GetStorageQuery(
    CalculationItemType Type,
    AuthorityStorage AuthoritySelected,
    StorageSort SortSelected
) : IRequest<IEnumerable<StorageDTO<object>>>;

public sealed class GetStorageQueryHandler(IShardingSingleDbContext _context)
    : IRequestHandler<GetStorageQuery, IEnumerable<StorageDTO<object>>>
{
    public async Task<IEnumerable<StorageDTO<object>>> Handle(GetStorageQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = _context.Storages
            .Where(x =>
                x.StorageType == request.Type &&
                x.StorageLevel == request.AuthoritySelected &&
                x.StorageSort == request.SortSelected)
            .AsNoTracking();
        // تحميل البيانات أولًا بدون تحويل JSON
        var data = await baseQuery.ToListAsync(cancellationToken);

        return data
            .Select(x => new StorageDTO<object>
            {
                Id = x.Id,
                StorageValue = request.Type == CalculationItemType.task
                    ? JsonSerializer.Deserialize<TaskStorageDTO>(x.StorageValue)
                    : JsonSerializer.Deserialize<ResourceStorageListDTO>(x.StorageValue),
                StorageLevel = x.StorageLevel,
                StorageSort = x.StorageSort,
                StorageType = x.StorageType
            });
    }
}
