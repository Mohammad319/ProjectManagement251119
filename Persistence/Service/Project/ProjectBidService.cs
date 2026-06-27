#nullable enable

using Application.Feature.Project.ProjectBid;
using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using Persistence.Service.Access;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Exceptions;
using ProjectManagement.Shared.Helper;
using System.Text.Json;

namespace Persistence.Service.Project
{
    public sealed class ProjectBidService(IDbContextFactoryTenant dbFactory) : IProjectBidService
    {
        /// <summary>Shown when a view-only user tries to change the tender evaluation.</summary>
        public const string ReadOnlyMessage = "Du har endast visningsåtkomst och kan inte ändra anbudsutvärderingen.";

        // True effective edit access for the tender evaluation = same rule as project content edit
        // (Admin / own department / creator / shared as "Kan ändra"). Tender evaluation is work data,
        // NOT project lifecycle management, so this deliberately does not require management rights.
        private static async Task<bool> CanEditAsync(
            Context.ShardingSingleDbContext context, Guid projectId, int userId, int? departmentId, CancellationToken ct) =>
            await context.Projects
                .Where(ProjectAccessRules.CanEdit(userId, departmentId))
                .AnyAsync(p => p.Id == projectId, ct);

        // Guards a write: a user without effective "Kan ändra" gets a clear 403 instead of a silent
        // "not found" — and never reaches the mutation. Blocks the "Kan visa" save attempt cleanly.
        private static async Task EnsureCanEditAsync(
            Context.ShardingSingleDbContext context, Guid projectId, int userId, int? departmentId, CancellationToken ct)
        {
            if (!await CanEditAsync(context, projectId, userId, departmentId, ct))
                throw new ForbiddenActionException(ReadOnlyMessage);
        }

        public async Task<ProjectBidsViewDTO> GetViewAsync(
            Guid projectId,
            int userId,
            int? departmentId,
            bool isViewer,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // Read access follows effective project access (CanSee), so a project shared with the
            // user (or their department) opens correctly. Without access we return an empty,
            // read-only view rather than leaking another department's data.
            var evaluation = await context.Projects
                .Where(ProjectAccessRules.CanSee(userId, departmentId, isViewer))
                .Where(x => x.Id == projectId)
                .Select(x => new { x.BidEvaluationModel, x.BidEvaluationBasis })
                .FirstOrDefaultAsync(ct);

            if (evaluation is null)
                return new ProjectBidsViewDTO { CanEdit = false };

            // Effective edit right (Kan ändra). Visare is always read-only.
            var canEdit = !isViewer && await CanEditAsync(context, projectId, userId, departmentId, ct);

            var evaluationModel = evaluation.BidEvaluationModel;
            var evaluationBasis = evaluation.BidEvaluationBasis;

            // Access already verified at project level above; load the project's parts/bids by id
            // (the tenant global filter still applies).
            var columns = await context.ProjectBidPriceColumns
                .Where(x => x.ProjectId == projectId)
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
                .Where(x => x.ProjectId == projectId)
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

            // Utvärderingsdelarnas tolkning styrs numera av modellen, inte av en typ
            // per del: i poängmodellen är varje del poäng, annars är varje del pris.
            var isPointsModel = evaluationModel == BidEvaluationModel.HighestPoints;
            var hasColumns = columns.Count > 0;

            var result = new ProjectBidsViewDTO
            {
                CanEdit = canEdit,
                EvaluationModel = evaluationModel,
                EvaluationBasis = evaluationBasis,
                PriceColumns = columns,
                Bids = bids.Select(x =>
                {
                    var prices = DeserializePrices(x.PricesJson, columnIds);
                    // Summan av delarna härleds vid läsning så att den alltid stämmer
                    // med aktuell modell (även efter att modellen bytts). Legacy-anbud
                    // utan delvärden faller tillbaka på det sparade beloppet.
                    var hasParts = hasColumns && prices.Count > 0;
                    var partsSum = prices.Values.Sum();

                    return new ProjectBidListDTO
                    {
                        Id = x.Id,
                        BidderName = x.BidderName,
                        Amount = isPointsModel ? null : (hasParts ? partsSum : x.Amount),
                        Prices = prices,
                        DeductionPercent = x.DeductionPercent,
                        Note = x.Note,
                        IsAwarded = x.IsAwarded,
                        ManualPlacement = x.Placement,
                        IsPlacementManuallyOverridden = x.Placement.HasValue,
                        Status = x.Status,
                        RejectionReason = x.RejectionReason,
                        TotalPoints = isPointsModel ? (hasParts ? partsSum : x.Amount) : null,
                        SortOrder = x.SortOrder
                    };
                }).ToList()
            };

            ProjectBidPlacementCalculator.Apply(result.Bids, evaluationModel);
            return result;
        }

        public async Task<List<ProjectBidComparisonRowDTO>> GetComparisonAsync(
            IReadOnlyList<Guid> projectIds,
            int userId,
            int? departmentId,
            bool isViewer,
            CancellationToken ct = default)
        {
            var requestedIds = projectIds?.Distinct().ToList() ?? [];
            if (requestedIds.Count == 0)
                return [];

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // Only include projects the user may actually see (effective access), so the comparison
            // never leaks bids from projects outside the user's access.
            var ids = await context.Projects
                .Where(ProjectAccessRules.CanSee(userId, departmentId, isViewer))
                .Where(x => requestedIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(ct);

            if (ids.Count == 0)
                return [];

            // Utvärderingsmodell per projekt (en samlad query).
            var models = await context.Projects
                .Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, x.BidEvaluationModel, x.BidEvaluationBasis })
                .ToListAsync(ct);

            var modelByProject = models.ToDictionary(x => x.Id, x => x.BidEvaluationModel);
            var basisByProject = models.ToDictionary(x => x.Id, x => x.BidEvaluationBasis);

            // Alla utvärderingsdelar per projekt (en samlad query). Modellen avgör hur
            // delarna tolkas vid läsning, så vi behöver inte längre filtrera på typ.
            var allColumns = await context.ProjectBidPriceColumns
                .Where(x => ids.Contains(x.ProjectId))
                .Select(x => new { x.ProjectId, x.Id })
                .ToListAsync(ct);

            var columnsByProject = allColumns
                .GroupBy(x => x.ProjectId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToHashSet());

            // Anbud för alla projekt (en samlad query) – undviker N+1.
            var bids = await context.ProjectBids
                .Where(x => ids.Contains(x.ProjectId))
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

            var result = bids.Select(x =>
            {
                var model = modelByProject.TryGetValue(x.ProjectId, out var m) ? m : BidEvaluationModel.LowestComparison;
                var isPointsModel = model == BidEvaluationModel.HighestPoints;
                var columnIds = columnsByProject.TryGetValue(x.ProjectId, out var c) ? c : null;
                var prices = DeserializePrices(x.PricesJson, columnIds);
                var hasParts = columnIds is { Count: > 0 } && prices.Count > 0;
                var partsSum = prices.Values.Sum();

                decimal? totalPoints = isPointsModel ? (hasParts ? partsSum : x.Amount) : null;
                decimal? amount = isPointsModel ? null : (hasParts ? partsSum : x.Amount);

                var comparison = amount.HasValue
                    ? amount.Value - amount.Value * (x.DeductionPercent ?? 0) / 100m
                    : (decimal?)null;

                return new ProjectBidComparisonRowDTO
                {
                    ProjectId = x.ProjectId,
                    EvaluationModel = model,
                    EvaluationBasis = basisByProject.TryGetValue(x.ProjectId, out var b) ? b : BidEvaluationBasis.Price,
                    BidId = x.Id,
                    BidderName = x.BidderName,
                    Amount = amount,
                    DeductionPercent = x.DeductionPercent,
                    ComparisonAmount = comparison,
                    TotalPoints = totalPoints,
                    IsAwarded = x.IsAwarded,
                    ManualPlacement = x.Placement,
                    IsPlacementManuallyOverridden = x.Placement.HasValue,
                    Status = x.Status,
                    RejectionReason = x.RejectionReason,
                    Note = x.Note,
                    SortOrder = x.SortOrder,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                };
            }).ToList();

            ProjectBidPlacementCalculator.Apply(result);
            return result;
        }

        public async Task<int> CreateAsync(
            Guid projectId,
            ProjectBidPostDTO dto,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            if (!IsValid(dto))
                return 0;

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            await EnsureCanEditAsync(context, projectId, userId, departmentId, ct);

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
            bid.SetManualPlacement(placement);

            context.ProjectBids.Add(bid);
            await context.SaveChangesAsync(ct);
            return bid.Id;
        }

        public async Task<bool> UpdateAsync(
            int id,
            Guid projectId,
            ProjectBidPostDTO dto,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            if (!IsValid(dto))
                return false;

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            await EnsureCanEditAsync(context, projectId, userId, departmentId, ct);

            var bid = await context.ProjectBids
                .FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId, ct);

            if (bid is null)
                return false;

            var (pricesJson, amount) = await ResolvePricesAsync(context, projectId, dto, ct);
            var (isAwarded, placement, status, rejectionReason) = NormalizeAward(dto);

            bid.Update(dto.BidderName, amount, dto.Note, isAwarded && status == BidStatus.Valid, placement);
            bid.SetPrices(pricesJson, amount);
            bid.SetDeductionPercent(NormalizeDeduction(dto.DeductionPercent));
            bid.SetStatus(status, rejectionReason);
            bid.SetManualPlacement(placement);

            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(
            int id,
            Guid projectId,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            await EnsureCanEditAsync(context, projectId, userId, departmentId, ct);

            var bid = await context.ProjectBids
                .FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId, ct);

            if (bid is null)
                return false;

            context.ProjectBids.Remove(bid);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> SetEvaluationAsync(
            Guid projectId,
            BidEvaluationBasis basis,
            BidEvaluationModel method,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            await EnsureCanEditAsync(context, projectId, userId, departmentId, ct);

            var project = await context.Projects
                .FirstOrDefaultAsync(x => x.Id == projectId, ct);

            if (project is null)
                return false;

            project.SetBidEvaluation(basis, method);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<int> CreatePriceColumnAsync(
            Guid projectId,
            ProjectBidPriceColumnPostDTO dto,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            var name = dto.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return 0;

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            await EnsureCanEditAsync(context, projectId, userId, departmentId, ct);

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
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            var name = dto.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return false;

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            await EnsureCanEditAsync(context, projectId, userId, departmentId, ct);

            var column = await FindColumnAsync(context, id, projectId, ct);
            if (column is null)
                return false;

            var nameTaken = await context.ProjectBidPriceColumns
                .AnyAsync(x => x.ProjectId == projectId && x.Id != id && x.Name.ToLower() == name.ToLower(), ct);

            if (nameTaken)
                return false;

            column.Rename(name);
            // Typfältet styrs inte längre från UI – modellen avgör tolkningen. Det
            // sparade värdet lämnas orört för bakåtkompatibilitet.
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeletePriceColumnAsync(
            int id,
            Guid projectId,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            await EnsureCanEditAsync(context, projectId, userId, departmentId, ct);

            var column = await FindColumnAsync(context, id, projectId, ct);
            if (column is null)
                return false;

            // Anbudssumma härleds bara i prismodellen; i poängmodellen finns ingen
            // prissumma och beloppet lämnas orört.
            var model = await context.Projects
                .Where(x => x.Id == projectId)
                .Select(x => x.BidEvaluationModel)
                .FirstOrDefaultAsync(ct);
            var isPriceModel = model == BidEvaluationModel.LowestComparison;

            // Remaining columns after this one is removed, so we can recompute each
            // bid's Anbudssumma without the deleted part.
            var remainingColumnIds = (await context.ProjectBidPriceColumns
                .Where(x => x.ProjectId == projectId && x.Id != id)
                .Select(x => x.Id)
                .ToListAsync(ct)).ToHashSet();

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
                    bid.SetPrices(null, isPriceModel ? null : bid.Amount);
                else
                    bid.SetPrices(
                        JsonSerializer.Serialize(prices),
                        isPriceModel ? SumPriceParts(prices, remainingColumnIds) : bid.Amount);
            }

            context.ProjectBidPriceColumns.Remove(column);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> MovePriceColumnAsync(
            int id,
            Guid projectId,
            int direction,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            if (direction is not (-1 or 1))
                return false;

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            await EnsureCanEditAsync(context, projectId, userId, departmentId, ct);

            var column = await FindColumnAsync(context, id, projectId, ct);
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

        // Edit access is verified by the caller (EnsureCanEditAsync); the column is then located by
        // id + project (tenant global filter still applies).
        private static async Task<ProjectBidPriceColumnEntity?> FindColumnAsync(
            Context.ShardingSingleDbContext context,
            int id,
            Guid projectId,
            CancellationToken ct) =>
            await context.ProjectBidPriceColumns
                .FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId, ct);

        private static bool IsValid(ProjectBidPostDTO dto)
        {
            if (dto.Amount is < 0)
                return false;
            if (dto.Prices is not null && dto.Prices.Values.Any(v => v < 0))
                return false;
            if (dto.DeductionPercent is < 0 or > 100)
                return false;
            if (dto.IsPlacementManuallyOverridden == true && dto.Placement is not > 0)
                return false;
            if (dto.IsPlacementManuallyOverridden is null && dto.Placement is < 1)
                return false;
            return true;
        }

        /// <summary>
        /// Förkastade anbud kan inte vara tilldelade. Placering är separat från tilldelning.
        /// </summary>
        private static (bool IsAwarded, int? Placement, BidStatus Status, string? RejectionReason) NormalizeAward(ProjectBidPostDTO dto)
        {
            var status = dto.Status;
            var rejectionReason = status == BidStatus.Rejected ? dto.RejectionReason : null;
            var isAwarded = dto.IsAwarded && status == BidStatus.Valid;
            var placement = status == BidStatus.Rejected || dto.IsPlacementManuallyOverridden == false
                ? null
                : dto.Placement is > 0 ? dto.Placement : null;
            return (isAwarded, placement, status, rejectionReason);
        }

        private static decimal? NormalizeDeduction(decimal? value) =>
            value is null or 0 ? null : value;

        private static decimal SumPriceParts(Dictionary<int, decimal> prices, HashSet<int> priceColumnIds) =>
            prices.Where(kv => priceColumnIds.Contains(kv.Key)).Sum(kv => kv.Value);

        /// <summary>
        /// When the project has evaluation parts and the client sent part values,
        /// the interpretation follows the project's evaluation model: in the price
        /// model (Lägst jämförelsesumma) every part is a price and Anbudssumma is their
        /// sum; in the points model (Högst totalpoäng) the parts are points and form no
        /// price sum (Amount stays null). Without parts the legacy single amount is kept
        /// so existing bids keep working unchanged.
        /// </summary>
        private static async Task<(string? PricesJson, decimal? Amount)> ResolvePricesAsync(
            Context.ShardingSingleDbContext context,
            Guid projectId,
            ProjectBidPostDTO dto,
            CancellationToken ct)
        {
            if (dto.Prices is null || dto.Prices.Count == 0)
                return (null, dto.Amount);

            var columnIds = (await context.ProjectBidPriceColumns
                .Where(x => x.ProjectId == projectId)
                .Select(x => x.Id)
                .ToListAsync(ct)).ToHashSet();

            var valid = dto.Prices
                .Where(kv => columnIds.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            if (valid.Count == 0)
                return (null, dto.Amount);

            var model = await context.Projects
                .Where(x => x.Id == projectId)
                .Select(x => x.BidEvaluationModel)
                .FirstOrDefaultAsync(ct);

            // Price model: Anbudssumma = summan av alla delar. Points model: ingen
            // prissumma – behåll ev. sänt belopp (null när delar finns).
            var amount = model == BidEvaluationModel.LowestComparison
                ? valid.Values.Sum()
                : dto.Amount;
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
