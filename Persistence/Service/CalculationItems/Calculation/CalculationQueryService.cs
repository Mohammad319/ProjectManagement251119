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
            bool isVisible,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var calculations = await context.Calculations
                .AsNoTracking()
                .Where(x => !x.IsDeleted &&
                    x.ProjectId == projectId &&
                    (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId.Value || x.CreatedBy == userId) &&
                    (!x.IsPrivate || x.CreatedBy == userId))
                .OrderBy(x => x.SortOrder)
                .Select(ListCalculationProjection)
                .ToListAsync(ct);

            var matchingFamilies = CalculationVersionSelector
                .SelectCurrentVersions(calculations)
                .Where(calculation => calculation.IsVisible == isVisible)
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
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.Calculations
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.ProjectId == projectId)
                .Where(x =>
                    departmentId == null ||
                    x.DepartmentId == departmentId.Value ||
                    x.SharesCalc.Any(s => s.DepartmentId == departmentId.Value))
                .Where(x =>
                    !x.IsPrivate ||
                    x.CreatedBy == userId ||
                    x.SharesCalc.Any(s => s.CreatedBy == userId || (departmentId != null && s.DepartmentId == departmentId.Value)))
                .OrderBy(x => x.SortOrder)
                .Select(x => new ListCalculationDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    Order = x.SortOrder,
                    IsPrivate = x.IsPrivate,
                    TenderDeadline = x.TenderDeadline,
                    TenderQA = x.TenderQA,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    Status = x.Status != null ? x.Status.Name : string.Empty,
                    StatusId = x.StatusId,
                    StatusSortOrder = x.Status != null ? x.Status.SortOrder : null,
                    StatusColor = x.Status != null ? x.Status.Color : string.Empty,
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
                    StatusAllowsProductionCalculation = x.Status != null && x.Status.AllowsProductionCalculation,
                    CountsAsSubmittedBid = x.Status != null && x.Status.CountsAsSubmittedBid,
                    CountsAsWonBid = x.Status != null && x.Status.CountsAsWonBid,
                    CountsAsLostBid = x.Status != null && x.Status.CountsAsLostBid,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    Tax = x.Tax,
                    IsVisible = x.IsVisible,
                    Inspector = x.Metadata.Inspector,
                    ProjectName = x.Project != null ? x.Project.Name : string.Empty,
                    FolderName = x.Project != null && x.Project.Folder != null ? x.Project.Folder.Name : string.Empty,
                    AddressText = x.Metadata.Address.Count > 0 ? x.Metadata.Address[0].Street : string.Empty,
                    Priority = x.Metadata.Priority,
                    TimeMonth = x.Metadata.TimeMonth
                })
                .ToListAsync(ct);
        }

        public async Task<CalculationDetailsDTO?> GetDetailsAsync(
            int id,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var calculation = await context.Calculations
                .AsNoTracking()
                .Include(x => x.Compensation)
                .Include(x => x.Contract)
                .Include(x => x.ProcurementMethods)
                .Include(x => x.Type)
                .Where(x => x.Id == id &&
                    (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId.Value || x.CreatedBy == userId) &&
                    (!x.IsPrivate || x.CreatedBy == userId))
                .FirstOrDefaultAsync(ct);

            return calculation?.ToDetailsDto();
        }

        public async Task<CalculationPostDTO?> GetPostModelAsync(
            int id,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var calculation = await context.Calculations
                .AsNoTracking()
                .Where(x => x.Id == id &&
                    (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId.Value || x.CreatedBy == userId) &&
                    (!x.IsPrivate || x.CreatedBy == userId))
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

        private static readonly System.Linq.Expressions.Expression<Func<CalculationEntity, ListCalculationDTO>>
            ListCalculationProjection =
                x => new ListCalculationDTO
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
                    Status = x.Status != null ? x.Status.Name : string.Empty,
                    StatusId = x.StatusId,
                    StatusSortOrder = x.Status != null ? x.Status.SortOrder : null,
                    StatusColor = x.Status != null ? x.Status.Color : string.Empty,
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
                    StatusAllowsProductionCalculation = x.Status != null && x.Status.AllowsProductionCalculation,
                    CountsAsSubmittedBid = x.Status != null && x.Status.CountsAsSubmittedBid,
                    CountsAsWonBid = x.Status != null && x.Status.CountsAsWonBid,
                    CountsAsLostBid = x.Status != null && x.Status.CountsAsLostBid,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    Tax = x.Tax,
                    IsVisible = x.IsVisible,
                    Inspector = x.Metadata.Inspector,
                    ProjectName = x.Project != null ? x.Project.Name : string.Empty,
                    FolderName = x.Project != null && x.Project.Folder != null ? x.Project.Folder.Name : string.Empty,
                    AddressText = x.Metadata.Address.Count > 0 ? x.Metadata.Address[0].Street : string.Empty,
                    Priority = x.Metadata.Priority,
                    TimeMonth = x.Metadata.TimeMonth
                };
    }
}
