using Application.Interfaces;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using System.Text.Json;

namespace Application.Services.CalculationItems.Storage
{
    public interface IStorageQueryService
    {
        Task<IEnumerable<StorageDTO<object>>> GetAsync(
            CalculationItemType type,
            AuthorityStorage level,
            StorageSort sort,
            CancellationToken ct = default);
    }


}
