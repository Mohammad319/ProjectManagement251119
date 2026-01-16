using Application.Services.CalculationItems.Storage;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using System.Text.Json;

namespace Persistence.Service.CalculationItems.Storage
{
    public sealed class StorageQueryService(IDbContextFactoryTenant dbFactory) : IStorageQueryService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public async Task<IEnumerable<StorageDTO<object>>> GetAsync(
            CalculationItemType type,
            AuthorityStorage level,
            StorageSort sort,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // Projection لتقليل الأعمدة
            var data = await context.Storages
                .AsNoTracking()
                .Where(x => x.StorageType == type &&
                            x.StorageLevel == level &&
                            x.StorageSort == sort)
                .Select(x => new
                {
                    x.Id,
                    x.StorageType,
                    x.StorageSort,
                    x.StorageLevel,
                    x.StorageValue
                })
                .ToListAsync(ct);

            // Deserialize
            var result = new List<StorageDTO<object>>(data.Count);

            if (type == CalculationItemType.task)
            {
                foreach (var x in data)
                {
                    result.Add(new StorageDTO<object>
                    {
                        Id = x.Id,
                        StorageType = x.StorageType,
                        StorageSort = x.StorageSort,
                        StorageLevel = x.StorageLevel,
                        StorageValue = JsonSerializer.Deserialize<TaskStorageDTO>(x.StorageValue, JsonOptions)
                                      ?? new TaskStorageDTO()
                    });
                }
            }
            else
            {
                foreach (var x in data)
                {
                    result.Add(new StorageDTO<object>
                    {
                        Id = x.Id,
                        StorageType = x.StorageType,
                        StorageSort = x.StorageSort,
                        StorageLevel = x.StorageLevel,
                        StorageValue = JsonSerializer.Deserialize<ResourceStorageListDTO>(x.StorageValue, JsonOptions)
                                      ?? new ResourceStorageListDTO()
                    });
                }
            }

            return result;
        }
    }
}
