using Application.Services.CalculationItems.Storage;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;
using System.Text.Json;

namespace Persistence.Service.CalculationItems.Storage
{
    public sealed class StorageQueryService(ShardingSingleDbContext db) : IStorageQueryService
    {
        public async Task<IEnumerable<StorageDTO<object>>> GetAsync(
            CalculationItemType type,
            AuthorityStorage level,
            StorageSort sort,
            CancellationToken ct = default)
        {
            var data = await db.Storages
                .Where(x =>
                    x.StorageType == type &&
                    x.StorageLevel == level &&
                    x.StorageSort == sort)
                .AsNoTracking()
                .ToListAsync(ct);

            return data.Select(x => new StorageDTO<object>
            {
                Id = x.Id,
                StorageType = x.StorageType,
                StorageSort = x.StorageSort,
                StorageLevel = x.StorageLevel,
                StorageValue = type == CalculationItemType.task
                    ? JsonSerializer.Deserialize<TaskStorageDTO>(x.StorageValue)!
                    : JsonSerializer.Deserialize<ResourceStorageListDTO>(x.StorageValue)!
            });
        }
    }

}
