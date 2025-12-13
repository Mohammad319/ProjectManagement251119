using Application.Extention;
using Application.Interfaces;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Application.Services.CalculationItems.Storage
{
    public interface IStorageCommandService
    {
        Task<bool> CreateAsync(
            CalculationItemType type,
            AuthorityStorage level,
            StorageSort sort,
            int id,
            int userId,
            int? departmentId,
            CancellationToken ct = default);

        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
