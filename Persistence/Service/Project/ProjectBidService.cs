#nullable enable

using Application.Feature.Project.ProjectBid;
using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Project;
using System.Text.Json;

namespace Persistence.Service.Project
{
    public sealed class ProjectBidService(IDbContextFactoryTenant dbFactory) : IProjectBidService
    {
        public async Task<ProjectBidsViewDTO> GetViewAsync(
            Guid projectId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var columns = await context.ProjectBidPriceColumns
                .Where(x => x.ProjectId == projectId &&
                    (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId.Value))
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedAt)
                .Select(x => new ProjectBidPriceColumnDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    SortOrder = x.SortOrder
                })
                .ToListAsync(ct);

            var bids = await context.ProjectBids
                .Where(x => x.ProjectId == projectId &&
                    (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId.Value))
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.BidderName,
                    x.Amount,
                    x.PricesJson,
                    x.DeductionPercent,
                    x.Note,
                    x.IsWinner,
                    x.SortOrder
                })
                .ToListAsync(ct);

            var columnIds = columns.Select(c => c.Id).ToHashSet();

            return new ProjectBidsViewDTO
            {
                PriceColumns = columns,
                Bids = bids.Select(x => new ProjectBidListDTO
                {
                    Id = x.Id,
                    BidderName = x.BidderName,
                    Amount = x.Amount,
                    Prices = DeserializePrices(x.PricesJson, columnIds),
                    DeductionPercent = x.DeductionPercent,
                    Note = x.Note,
                    IsWinner = x.IsWinner,
                    SortOrder = x.SortOrder
                }).ToList()
            };
        }

        public async Task<int> CreateAsync(
            Guid projectId,
            ProjectBidPostDTO dto,
            int? departmentId,
            CancellationToken ct = default)
        {
            if (!IsValid(dto))
                return 0;

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var projectExists = await context.Projects
                .AnyAsync(x => x.Id == projectId &&
                    (!departmentId.HasValue || x.Folder.DepartmentId == departmentId.Value), ct);

            if (!projectExists)
                return 0;

            var sortOrder = await context.ProjectBids
                .Where(x => x.ProjectId == projectId)
                .MaxAsync(x => (int?)x.SortOrder, ct) ?? 0;

            var (pricesJson, amount) = await ResolvePricesAsync(context, projectId, dto, ct);

            var bid = new ProjectBidEntity(
                projectId: projectId,
                bidderName: dto.BidderName,
                amount: amount,
                note: dto.Note,
                isWinner: dto.IsWinner,
                sortOrder: sortOrder + 100);

            bid.SetPrices(pricesJson, amount);
            bid.SetDeductionPercent(NormalizeDeduction(dto.DeductionPercent));

            context.ProjectBids.Add(bid);
            await context.SaveChangesAsync(ct);
            return bid.Id;
        }

        public async Task<bool> UpdateAsync(
            int id,
            Guid projectId,
            ProjectBidPostDTO dto,
            int? departmentId,
            CancellationToken ct = default)
        {
            if (!IsValid(dto))
                return false;

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var bid = await context.ProjectBids
                .FirstOrDefaultAsync(x => x.Id == id &&
                    x.ProjectId == projectId &&
                    (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId.Value), ct);

            if (bid is null)
                return false;

            var (pricesJson, amount) = await ResolvePricesAsync(context, projectId, dto, ct);

            bid.Update(dto.BidderName, amount, dto.Note, dto.IsWinner);
            bid.SetPrices(pricesJson, amount);
            bid.SetDeductionPercent(NormalizeDeduction(dto.DeductionPercent));

            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(
            int id,
            Guid projectId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var bid = await context.ProjectBids
                .FirstOrDefaultAsync(x => x.Id == id &&
                    x.ProjectId == projectId &&
                    (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId.Value), ct);

            if (bid is null)
                return false;

            context.ProjectBids.Remove(bid);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<int> CreatePriceColumnAsync(
            Guid projectId,
            ProjectBidPriceColumnPostDTO dto,
            int? departmentId,
            CancellationToken ct = default)
        {
            var name = dto.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return 0;

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var projectExists = await context.Projects
                .AnyAsync(x => x.Id == projectId &&
                    (!departmentId.HasValue || x.Folder.DepartmentId == departmentId.Value), ct);

            if (!projectExists)
                return 0;

            var nameTaken = await context.ProjectBidPriceColumns
                .AnyAsync(x => x.ProjectId == projectId && x.Name.ToLower() == name.ToLower(), ct);

            if (nameTaken)
                return 0;

            var sortOrder = await context.ProjectBidPriceColumns
                .Where(x => x.ProjectId == projectId)
                .MaxAsync(x => (int?)x.SortOrder, ct) ?? 0;

            var column = new ProjectBidPriceColumnEntity(projectId, name, sortOrder + 100);
            context.ProjectBidPriceColumns.Add(column);
            await context.SaveChangesAsync(ct);
            return column.Id;
        }

        public async Task<bool> RenamePriceColumnAsync(
            int id,
            Guid projectId,
            ProjectBidPriceColumnPostDTO dto,
            int? departmentId,
            CancellationToken ct = default)
        {
            var name = dto.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return false;

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var column = await FindColumnAsync(context, id, projectId, departmentId, ct);
            if (column is null)
                return false;

            var nameTaken = await context.ProjectBidPriceColumns
                .AnyAsync(x => x.ProjectId == projectId && x.Id != id && x.Name.ToLower() == name.ToLower(), ct);

            if (nameTaken)
                return false;

            column.Rename(name);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeletePriceColumnAsync(
            int id,
            Guid projectId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var column = await FindColumnAsync(context, id, projectId, departmentId, ct);
            if (column is null)
                return false;

            // Strip this column's values from all bids and recompute their totals
            // so no stale amounts survive the deletion.
            var bidsWithPrices = await context.ProjectBids
                .Where(x => x.ProjectId == projectId && x.PricesJson != null)
                .ToListAsync(ct);

            foreach (var bid in bidsWithPrices)
            {
                var prices = DeserializePrices(bid.PricesJson, columnIds: null);
                if (!prices.Remove(id))
                    continue;

                if (prices.Count == 0)
                    bid.SetPrices(null, null);
                else
                    bid.SetPrices(JsonSerializer.Serialize(prices), prices.Values.Sum());
            }

            context.ProjectBidPriceColumns.Remove(column);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> MovePriceColumnAsync(
            int id,
            Guid projectId,
            int direction,
            int? departmentId,
            CancellationToken ct = default)
        {
            if (direction is not (-1 or 1))
                return false;

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var column = await FindColumnAsync(context, id, projectId, departmentId, ct);
            if (column is null)
                return false;

            var ordered = await context.ProjectBidPriceColumns
                .Where(x => x.ProjectId == projectId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedAt)
                .ToListAsync(ct);

            var index = ordered.FindIndex(x => x.Id == id);
            var targetIndex = index + direction;
            if (index < 0 || targetIndex < 0 || targetIndex >= ordered.Count)
                return false;

            var current = ordered[index];
            var target = ordered[targetIndex];

            // Normalize first in case legacy rows share the same SortOrder value.
            if (current.SortOrder == target.SortOrder)
                for (var i = 0; i < ordered.Count; i++)
                    ordered[i].SetSortOrder((i + 1) * 100);

            (var a, var b) = (current.SortOrder, target.SortOrder);
            current.SetSortOrder(b);
            target.SetSortOrder(a);

            await context.SaveChangesAsync(ct);
            return true;
        }

        private static async Task<ProjectBidPriceColumnEntity?> FindColumnAsync(
            Context.ShardingSingleDbContext context,
            int id,
            Guid projectId,
            int? departmentId,
            CancellationToken ct) =>
            await context.ProjectBidPriceColumns
                .FirstOrDefaultAsync(x => x.Id == id &&
                    x.ProjectId == projectId &&
                    (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId.Value), ct);

        private static bool IsValid(ProjectBidPostDTO dto)
        {
            if (dto.Amount is < 0)
                return false;
            if (dto.Prices is not null && dto.Prices.Values.Any(v => v < 0))
                return false;
            if (dto.DeductionPercent is < 0 or > 100)
                return false;
            return true;
        }

        private static decimal? NormalizeDeduction(decimal? value) =>
            value is null or 0 ? null : value;

        /// <summary>
        /// When the project has price columns and the client sent price parts,
        /// the total (Anbudssumma) is the sum of the parts. Otherwise the legacy
        /// single amount is kept so existing bids keep working unchanged.
        /// </summary>
        private static async Task<(string? PricesJson, decimal? Amount)> ResolvePricesAsync(
            Context.ShardingSingleDbContext context,
            Guid projectId,
            ProjectBidPostDTO dto,
            CancellationToken ct)
        {
            if (dto.Prices is null || dto.Prices.Count == 0)
                return (null, dto.Amount);

            var columnIds = await context.ProjectBidPriceColumns
                .Where(x => x.ProjectId == projectId)
                .Select(x => x.Id)
                .ToListAsync(ct);

            var valid = dto.Prices
                .Where(kv => columnIds.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            if (valid.Count == 0)
                return (null, dto.Amount);

            return (JsonSerializer.Serialize(valid), valid.Values.Sum());
        }

        private static Dictionary<int, decimal> DeserializePrices(string? json, HashSet<int>? columnIds)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<int, decimal>();

            try
            {
                var prices = JsonSerializer.Deserialize<Dictionary<int, decimal>>(json) ?? new Dictionary<int, decimal>();
                return columnIds is null
                    ? prices
                    : prices.Where(kv => columnIds.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
            }
            catch (JsonException)
            {
                return new Dictionary<int, decimal>();
            }
        }
    }
}
