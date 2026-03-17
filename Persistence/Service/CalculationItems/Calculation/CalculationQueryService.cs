namespace Persistence.Service.CalculationItems.Calculation
{
    using Domain.Entities.Calculation;
    using global::Application.Feature.Calculation.Calculation;
    using Microsoft.EntityFrameworkCore;
    using Persistence.Factory;
    using ProjectManagement.Shared.DTO.Calculation;
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

            return await context.Calculations
                .AsNoTracking()
                .Where(x => !x.IsDeleted &&
                    x.ProjectId == projectId &&
                    x.IsVisible == isVisible &&
                    (!x.IsPrivate || x.CreatedBy == userId))
                .OrderBy(x => x.SortOrder)
                .Select(ListCalculationProjection)
                .ToListAsync(ct);
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
                    Status = x.Status != null ? x.Status.Name : string.Empty
                })
                .ToListAsync(ct);
        }

        public async Task<CalculationDetailsDTO?> GetDetailsAsync(
            int id,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.Calculations
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new CalculationDetailsDTO
                {
                    TenderQA = x.TenderQA,
                    TenderDeadline = x.TenderDeadline,
                    Priority = x.Metadata.Priority,
                    Procurement = x.Procurement,
                    Name = x.Name,
                    Tax = x.Tax,
                    TimeMonth = x.Metadata.TimeMonth,
                    Compensation = x.Compensation != null ? x.Compensation.Name : string.Empty,
                    Contract = x.Contract != null ? x.Contract.Name : string.Empty,
                    ProcurementMethods = x.ProcurementMethods != null ? x.ProcurementMethods.Name : string.Empty,
                    Type = x.Type != null ? x.Type.Name : string.Empty,
                    Order = x.SortOrder,
                    ClientsManager = x.Metadata.ClientsManager,
                    Code = x.Code,
                    ContactPerson = x.Metadata.ContactPerson,
                    Contacts = x.Metadata.Contacts,
                    Address = x.Metadata.Address,
                    DecisionDate = x.DecisionDate,
                    Designer = x.Metadata.Designer,
                    Developer = x.Metadata.Developer,
                    EndDate = x.EndDate,
                    Income = x.Metadata.Income,
                    StartDate = x.StartDate,
                    Supervisor = x.Metadata.Supervisor,
                    PublicationDate = x.PublicationDate,
                    HourlyPrice = x.HourlyPrice,
                    Maps = x.Metadata.Maps,
                    Notes = x.Metadata.Notes,
                    Inspector = x.Metadata.Inspector,
                    OverviewInfo = x.Metadata.OverviewInfo,
                    Responsibles = x.Metadata.Responsibles
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<CalculationPostDTO?> GetPostModelAsync(
            int id,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.Calculations
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new CalculationPostDTO
                {
                    TenderQA = x.TenderQA,
                    TenderDeadline = x.TenderDeadline,
                    CompensationId = x.CompensationId,
                    ContractId = x.ContractId,
                    Procurement = x.Procurement,
                    ProcurementMethodsId = x.ProcurementMethodsId,
                    Name = x.Name,
                    Tax = x.Tax,
                    TypeId = x.TypeId,
                    IsPrivate = x.IsPrivate,
                    Code = x.Code,
                    OrganisationId = x.OrganisationId,
                    EndDate = x.EndDate,
                    StatusId = x.StatusId,
                    IsVisible = x.IsVisible,
                    StartDate = x.StartDate,
                    TimeMonth = x.Metadata.TimeMonth,
                    Order = x.SortOrder,
                    ClientsManager = x.Metadata.ClientsManager,
                    ContactPerson = x.Metadata.ContactPerson,
                    Contacts = x.Metadata.Contacts,
                    Address = x.Metadata.Address,
                    DecisionDate = x.DecisionDate,
                    Designer = x.Metadata.Designer,
                    Developer = x.Metadata.Developer,
                    Income = x.Metadata.Income,
                    Supervisor = x.Metadata.Supervisor,
                    PublicationDate = x.PublicationDate,
                    HourlyPrice = x.HourlyPrice,
                    Maps = x.Metadata.Maps,
                    Notes = x.Metadata.Notes,
                    Inspector = x.Metadata.Inspector,
                    OverviewInfo = x.Metadata.OverviewInfo,
                    Responsibles = x.Metadata.Responsibles,
                    Priority = x.Metadata.Priority
                })
                .FirstOrDefaultAsync(ct);
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
                    Status = x.Status != null ? x.Status.Name : string.Empty
                };
    }
}
