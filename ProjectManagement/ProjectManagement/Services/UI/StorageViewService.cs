using Application.Feature.Calculation.Storage.Queries;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Services.UI;

public interface IStorageViewService
{
    Task<List<StorageDTO<TaskPostDTO>>> GetTasksAsync(GetFilterDTO filter, CancellationToken ct = default);
    Task<List<StorageDTO<ResourcePostDTO>>> GetResourcesAsync(GetFilterDTO filter, CancellationToken ct = default);
}

public sealed class StorageViewService(ICommandDispatcher dispatcher) : IStorageViewService
{
    public async Task<List<StorageDTO<TaskPostDTO>>> GetTasksAsync(GetFilterDTO filter, CancellationToken ct = default)
    {
        var items = await dispatcher.Send(new GetStorageQuery(filter.Type, filter.AuthoritySelected, filter.SortSelected), ct);

        return items?
            .Where(x => x.StorageValue is TaskStorageDTO)
            .Select(x => new StorageDTO<TaskPostDTO>
            {
                Id = x.Id,
                StorageType = x.StorageType,
                StorageSort = x.StorageSort,
                StorageLevel = x.StorageLevel,
                StorageValue = MapTask((TaskStorageDTO)x.StorageValue)
            })
            .ToList() ?? [];
    }

    public async Task<List<StorageDTO<ResourcePostDTO>>> GetResourcesAsync(GetFilterDTO filter, CancellationToken ct = default)
    {
        var items = await dispatcher.Send(new GetStorageQuery(filter.Type, filter.AuthoritySelected, filter.SortSelected), ct);

        return items?
            .Where(x => x.StorageValue is ResourceStorageListDTO)
            .Select(x => new StorageDTO<ResourcePostDTO>
            {
                Id = x.Id,
                StorageType = x.StorageType,
                StorageSort = x.StorageSort,
                StorageLevel = x.StorageLevel,
                StorageValue = MapResource((ResourceStorageListDTO)x.StorageValue)
            })
            .ToList() ?? [];
    }

    private static TaskPostDTO MapTask(TaskStorageDTO source)
        => new()
        {
            Id = source.Id,
            Name = source.Name,
            Order = source.Order,
            Metadata = source.Data?.Clone() ?? new TaskMetadata(),
            Resources = source.Resources?.Select(MapResource).ToList() ?? [],
            Tasks = source.Tasks?.Select(MapTask).ToList() ?? []
        };

    private static ResourcePostDTO MapResource(ResourceStorageListDTO source)
        => new()
        {
            Id = source.Id,
            Name = source.Name,
            Order = source.Order,
            ResType = source.ResType,
            IsActive = source.Active,
            Data = source.Data?.Clone() ?? new ResourceMetadata()
        };
}
