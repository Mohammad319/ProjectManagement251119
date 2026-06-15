#nullable enable

using Application.Feature.Project.ProjectBid;
using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;
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

            var evaluationModel = await context.Projects
                .Where(x => x.Id == projectId &&
                    (!departmentId.HasValue || x.Folder.DepartmentId == departmentId.Value))
                .Select(x => (BidEvaluationModel?)x.BidEvaluationModel)
                .FirstOrDefaultAsync(ct) ?? BidEvaluationModel.LowestComparison;

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
                    PartType = x.PartType,
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
                    x.IsAwarded,
                    x.Placement,
                    x.Status,
                    x.RejectionReason,
                    x.SortOrder
                })
                .ToListAsync(ct);

            var columnIds = columns.Select(c => c.Id).ToHashSet();
            var pointColumnIds = columns.Where(c => c.PartType == BidPartType.Points).Select(c => c.Id).ToHashSet();
            var hasPointColumns = pointColumnIds.Count > 0;

            return new ProjectBidsViewDTO
            {
                EvaluationModel = evaluationModel,
                PriceColumns = columns,
                Bids = bids.Select(x =>
                {
                    var prices = DeserializePrices(x.PricesJson, columnIds);
                    return new ProjectBidListDTO
                    {
                        Id = x.Id,
                        BidderName = x.BidderName,
                        Amount = x.Amount,
                        Prices = prices,
                        DeductionPercent = x.DeductionPercent,
                        Note = x.Note,
                        IsAwarded = x.IsAwarded,
                        Placement = x.Placement,
                        Status = x.Status,
                        RejectionReason = x.RejectionReason,
                        TotalPoints = hasPointColumns
                            ? prices.Where(kv => pointColumnIds.Contains(kv.Key)).Sum(kv => kv.Value)
                            : null,
                        SortOrder = x.SortOrder
                    };
                }).ToList()
            };
        }

        public async Task<List<ProjectBidComparisonRowDTO>> GetComparisonAsync(
            IReadOnlyList<Guid> projectIds,
            int? departmentId,
            CancellationToken ct = default)
        {
            var ids = projectIds?.Distinct().ToList() ?? [];
            if (ids.Count == 0)
                return [];

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // Utvärderingsmodell per projekt (en samlad query).
            var models = await context.Projects
                .Where(x => ids.Contains(x.Id) &&
                    (!departmentId.HasValue || x.Folder.DepartmentId == departmentId.Value))
                .Select(x => new { x.Id, x.BidEvaluationModel })
                .ToListAsync(ct);

            var modelByProject = models.ToDictionary(x => x.Id, x => x.BidEvaluationModel);

            // Poängdelar per projekt (en samlad query) – behövs för totalpoäng.
            var pointColumns = await context.ProjectBidPriceColumns
                .Where(x => ids.Contains(x.ProjectId) && x.PartType == BidPartType.Points &&
                    (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId.Value))
                .Select(x => new { x.ProjectId, x.Id })
                .ToListAsync(ct);

            var pointColumnsByProject = pointColumns
                .GroupBy(x => x.ProjectId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToHashSet());

            // Anbud för alla projekt (en samlad query) – undviker N+1.
            var bids = await context.ProjectBids
                .Where(x => ids.Contains(x.ProjectId) &&
                    (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId.Value))
                .AsNoTracking()
                .OrderBy(x => x.ProjectId)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.ProjectId,
                    x.BidderName,
                    x.Amount,
                    x.PricesJson,
                    x.DeductionPercent,
                    x.Note,
                    x.IsAwarded,
                    x.Placement,
                    x.Status,
                    x.RejectionReason,
                    x.SortOrder,
                    x.CreatedAt,
                    x.UpdatedAt
                })
                .ToListAsync(ct);

            return bids.Select(x =>
            {
                var pointIds = pointColumnsByProject.TryGetValue(x.ProjectId, out var p) ? p : null;
                decimal? totalPoints = null;
                if (pointIds is { Count: > 0 })
                {
                    var prices = DeserializePrices(x.PricesJson, columnIds: null);
                    totalPoints = prices.Where(kv => pointIds.Contains(kv.Key)).Sum(kv => kv.Value);
                }

                var comparison = x.Amount.HasValue
                    ? x.Amount.Value - x.Amount.Value * (x.DeductionPercent ?? 0) / 100m
                    : (decimal?)null;

                return new ProjectBidComparisonRowDTO
                {
                    ProjectId = x.ProjectId,
                    EvaluationModel = modelByProject.TryGetValue(x.ProjectId, out var m) ? m : BidEvaluationModel.LowestComparison,
                    BidId = x.Id,
                    BidderName = x.BidderName,
                    Amount = x.Amount,
                    DeductionPercent = x.DeductionPercent,
                    ComparisonAmount = comparison,
                    TotalPoints = totalPoints,
                    IsAwarded = x.IsAwarded,
                    Placement = x.Placement,
                    Status = x.Status,
                    RejectionReason = x.RejectionReason,
                    Note = x.Note,
                    SortOrder = x.SortOrder,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                };
            }).ToList();
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
            var (isAwarded, placement, status, rejectionReason) = NormalizeAward(dto);

            var bid = new ProjectBidEntity(
                projectId: projectId,
                bidderName: dto.BidderName,
                amount: amount,
                note: dto.Note,
                isAwarded: isAwarded,
                sortOrder: sortOrder + 100);

            bid.SetPrices(pricesJson, amount);
            bid.SetDeductionPercent(NormalizeDeduction(dto.DeductionPercent));
            bid.SetStatus(status, rejectionReason);
            bid.SetAwarded(isAwarded && status == BidStatus.Valid, placement);

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
            var (isAwarded, placement, status, rejectionReason) = NormalizeAward(dto);

            bid.Update(dto.BidderName, amount, dto.Note, isAwarded && status == BidStatus.Valid, placement);
            bid.SetPrices(pricesJson, amount);
            bid.SetDeductionPercent(NormalizeDeduction(dto.DeductionPercent));
            bid.SetStatus(status, rejectionReason);

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

        public async Task<bool> SetEvaluationModelAsync(
            Guid projectId,
            BidEvaluationModel model,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var project = await context.Projects
                .FirstOrDefaultAsync(x => x.Id == projectId &&
                    (!departmentId.HasValue || x.Folder.DepartmentId == departmentId.Value), ct);

            if (project is null)
                return false;

            project.SetBidEvaluationModel(model);
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

            var column = new ProjectBidPriceColumnEntity(projectId, name, sortOrder + 100, dto.PartType);
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
            column.SetPartType(dto.PartType);
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

            // Which columns are price parts after this one is removed, so we can
            // recompute each bid's Anbudssumma without the deleted part.
            var priceColumnIds = await context.ProjectBidPriceColumns
                .Where(x => x.ProjectId == projectId && x.Id != id && x.PartType == BidPartType.Price)
                .Select(x => x.Id)
                .ToListAsync(ct);

            // Strip this column's values from all bids and recompute their totals
            // so no stale amounts survive the deletion.
            var bidsWithPrices = await context.ProjectBids
                .Where(x => x.ProjectId == projectId && x.PricesJson != null)
                .ToListAsync(ct);

            var priceColumnSet = priceColumnIds.ToHashSet();

            foreach (var bid in bidsWithPrices)
            {
                var prices = DeserializePrices(bid.PricesJson, columnIds: null);
                if (!prices.Remove(id))
                    continue;

                if (prices.Count == 0)
                    bid.SetPrices(null, null);
                else
                    bid.SetPrices(JsonSerializer.Serialize(prices), SumPriceParts(prices, priceColumnSet));
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
            if (dto.Placement is < 1)
                return false;
            return true;
        }

        /// <summary>
        /// Förkastade anbud kan inte vara tilldelade. Tilldelning kräver giltigt anbud
        /// och placering behålls bara för tilldelade anbud.
        /// </summary>
        private static (bool IsAwarded, int? Placement, BidStatus Status, string? RejectionReason) NormalizeAward(ProjectBidPostDTO dto)
        {
            var status = dto.Status;
            var rejectionReason = status == BidStatus.Rejected ? dto.RejectionReason : null;
            var isAwarded = dto.IsAwarded && status == BidStatus.Valid;
            var placement = isAwarded && dto.Placement is > 0 ? dto.Placement : null;
            return (isAwarded, placement, status, rejectionReason);
        }

        private static decimal? NormalizeDeduction(decimal? value) =>
            value is null or 0 ? null : value;

        private static decimal SumPriceParts(Dictionary<int, decimal> prices, HashSet<int> priceColumnIds) =>
            prices.Where(kv => priceColumnIds.Contains(kv.Key)).Sum(kv => kv.Value);

        /// <summary>
        /// When the project has evaluation parts and the client sent part values,
        /// the total (Anbudssumma) is the sum of the <b>price</b> parts only — point
        /// parts are stored alongside but don't affect the bid sum. Otherwise the legacy
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

            var columns = await context.ProjectBidPriceColumns
                .Where(x => x.ProjectId == projectId)
                .Select(x => new { x.Id, x.PartType })
                .ToListAsync(ct);

            var columnIds = columns.Select(c => c.Id).ToHashSet();
            var priceColumnIds = columns.Where(c => c.PartType == BidPartType.Price).Select(c => c.Id).ToHashSet();

            var valid = dto.Prices
                .Where(kv => columnIds.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            if (valid.Count == 0)
                return (null, dto.Amount);

            // Anbudssumma is the sum of the price parts. If the project only has
            // point parts there is no price sum, so fall back to any sent Amount.
            var amount = priceColumnIds.Count > 0 ? SumPriceParts(valid, priceColumnIds) : dto.Amount;
            return (JsonSerializer.Serialize(valid), amount);
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
