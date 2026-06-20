namespace Persistence.Service.CalculationItems.Calculation
{
    using global::Application.Mapping.Calculation;
    using Domain.Entities.Calculation;
    using global::Application.Feature.Calculation.Calculation;
    using Microsoft.EntityFrameworkCore;
    using Persistence.Factory;
    using ProjectManagement.Shared.DTO.Calculation;
    using ProjectManagement.Shared.Helper;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed partial class CalculationQueryService(IDbContextFactoryTenant dbFactory) : ICalculationQueryService
    {
        public async Task<IReadOnlyList<ListCalculationDTO>> GetAllAsync(
            Guid projectId,
            bool isArchived,
            int userId,
            int? departmentId,
            CancellationToken ct = default,
            bool isViewer = false)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var calculationEntities = await context.Calculations
                .AsNoTracking()
                .Include(x => x.Status)
                .Include(x => x.Project)
                    .ThenInclude(x => x.Folder)
                .Where(x => !x.IsDeleted && x.ProjectId == projectId)
                .Where(Access.CalculationAccessRules.CanSee(userId, departmentId, isViewer))
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            var calculations = calculationEntities.Select(ToListCalculationDto).ToList();

            var matchingFamilies = CalculationVersionSelector
                .SelectCurrentVersions(calculations)
                .Where(calculation => calculation.IsArchived == isArchived)
                .Select(CalculationVersionSelector.GetFamilyKey)
                .ToHashSet();

            return calculations
                .Where(calculation => matchingFamilies.Contains(
                    CalculationVersionSelector.GetFamilyKey(calculation)))
                .ToList();
        }

        public async Task<IEnumerable<ListCalculationDTO>> GetByDepartmentAsync(
            Guid projectId,
            int userId,
            int? departmentId,
            CancellationToken ct = default,
            bool isViewer = false)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            IQueryable<Domain.Entities.Calculation.CalculationEntity> query = context.Calculations
                .AsNoTracking()
                .Include(x => x.Status)
                .Include(x => x.Project)
                    .ThenInclude(x => x.Folder)
                .Where(x => !x.IsDeleted && x.ProjectId == projectId);

            // Visare: bara kalkyler valda i en projektdelning, aldrig privata.
            // Övriga: befintlig delningslogik via kalkyldelning (ShareCalc).
            query = isViewer
                ? query.Where(Access.CalculationAccessRules.CanSee(userId, departmentId, isViewerOnly: true))
                : query
                    .Where(x =>
                        departmentId == null ||
                        x.DepartmentId == departmentId.Value ||
                        x.SharesCalc.Any(s => s.DepartmentId == departmentId.Value))
                    .Where(x =>
                        !x.IsPrivate ||
                        x.CreatedBy == userId ||
                        departmentId == null ||
                        x.SharesCalc.Any(s => s.CreatedBy == userId || (departmentId != null && s.DepartmentId == departmentId.Value)));

            var calculationEntities = await query
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            return calculationEntities.Select(ToListCalculationDto).ToList();
        }

        public async Task<CalculationDetailsDTO?> GetDetailsAsync(
            int id,
            int userId,
            int? departmentId,
            CancellationToken ct = default,
            bool isViewer = false)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var calculation = await context.Calculations
                .AsNoTracking()
                .Include(x => x.Compensation)
                .Include(x => x.Contract)
                .Include(x => x.ProcurementMethods)
                .Include(x => x.Type)
                .Where(x => x.Id == id)
                .Where(Access.CalculationAccessRules.CanSee(userId, departmentId, isViewer))
                .FirstOrDefaultAsync(ct);

            return calculation?.ToDetailsDto();
        }

        public async Task<CalculationPostDTO?> GetPostModelAsync(
            int id,
            int userId,
            int? departmentId,
            CancellationToken ct = default,
            bool isViewer = false)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var calculation = await context.Calculations
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Where(Access.CalculationAccessRules.CanSee(userId, departmentId, isViewer))
                .FirstOrDefaultAsync(ct);

            return calculation?.ToPostDto();
        }

        public async Task<List<HourlyPriceListGroupDTO>> GetHourlyPriceListAsync(
            int id,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var priceList = await context.Calculations
                .AsNoTracking()
                .Where(x => x.Id == id &&
                            (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId.Value))
                .FirstOrDefaultAsync(ct);

            return priceList?.HourlyPrice ?? [];
        }

        private static ListCalculationDTO ToListCalculationDto(CalculationEntity x) => new()
        {
            Id = x.Id,
            Name = x.Name,
            Code = x.Code,
            Order = x.SortOrder,
            TenderDeadline = x.TenderDeadline,
            TenderQA = x.TenderQA,
            IsPrivate = x.IsPrivate,
            EndDate = x.EndDate,
            StartDate = x.StartDate,
            Status = x.Status?.Name ?? string.Empty,
            StatusId = x.StatusId,
            StatusSortOrder = x.Status?.SortOrder,
            StatusColor = x.Status?.Color ?? string.Empty,
            CalculationType = x.CalculationType,
            BidRole = x.BidRole,
            CalculationRole = x.CalculationRole,
            CustomCalculationRoleName = x.CustomCalculationRoleName,
            IsLocked = x.IsLocked,
            LockedAtUtc = x.LockedAtUtc,
            LockedByUserId = x.LockedByUserId,
            ApprovedByUserId = x.ApprovedByUserId,
            ApprovedByName = x.ApprovedByName,
            ApprovedAtUtc = x.ApprovedAtUtc,
            SourceCalculationId = x.SourceCalculationId,
            VersionGroupId = x.VersionGroupId,
            VersionNumber = x.VersionNumber,
            CreatedFromCalculationId = x.CreatedFromCalculationId,
            IsCurrentVersion = x.IsCurrentVersion,
            StatusAllowsProductionCalculation = x.Status?.AllowsProductionCalculation ?? false,
            CountsAsSubmittedBid = x.Status?.CountsAsSubmittedBid ?? false,
            CountsAsWonBid = x.Status?.CountsAsWonBid ?? false,
            CountsAsLostBid = x.Status?.CountsAsLostBid ?? false,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            Tax = x.Tax,
            IsArchived = x.IsArchived,
            Inspector = x.Metadata.Inspector,
            ProjectName = x.Project?.Name ?? string.Empty,
            FolderName = x.Project?.Folder?.Name ?? string.Empty,
            AddressText = FormatAddress(x.Metadata.Address.FirstOrDefault()),
            Priority = x.Metadata.Priority,
            TimeMonth = x.Metadata.TimeMonth,
            ImportInfo = x.Metadata.ImportInfo
        };

        private static string FormatAddress(ProjectManagement.Shared.DTO.App.AddressDTO? address)
        {
            if (address is null)
                return string.Empty;

            var parts = new[]
            {
                address.Street,
                address.Nr,
                address.ZIPCode,
                address.City,
                address.Region,
                address.Country
            };

            return string.Join(", ", parts.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        }
    }
}
