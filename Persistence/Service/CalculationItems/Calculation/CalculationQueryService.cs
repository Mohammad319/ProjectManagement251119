namespace Persistence.Service.CalculationItems
{
    using global::Application.Feature.Calculation.Calculation;
    using Microsoft.EntityFrameworkCore;
    using Persistence.Factory;
    using ProjectManagement.Shared.DTO.Calculation;
    using ProjectManagement.Shared.DTO.Offer;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class CalculationQueryService(IDbContextFactoryTenant dbFactory) : ICalculationQueryService
    {
        // -------------------------------------------------
        // GetAllCalculations (بنفس منطق GetAllCalculationsQuery)
        // -------------------------------------------------
        public async Task<IReadOnlyList<ListCalculationDTO>> GetAllAsync(
            Guid projectId,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.Calculations
                .AsNoTracking()
                .Where(x =>
                    x.ProjectId == projectId &&
                    (departmentId == null || x.Project.Folder.DepartmentId == departmentId) &&
                    (!x.IsPrivate || x.CreatedBy == userId))
                .OrderBy(x => x.SortOrder)
                .Select(x => new ListCalculationDTO
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
                    Status = x.Status.Name
                })
                .ToListAsync(ct);
        }

        // -------------------------------------------------
        // GetAllCalculationsByDepartment
        // -------------------------------------------------
        public async Task<IEnumerable<ListCalculationDTO>> GetByDepartmentAsync(
            Guid projectId,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.Calculations
                .AsNoTracking()
                .Where(x =>
                    x.ProjectId == projectId &&
                    (departmentId == null ||
                     x.SharesCalc.Any(s => s.CreatedBy == userId || s.DepartmentId == departmentId)))
                .Select(x => new ListCalculationDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Order = x.SortOrder,
                    IsPrivate = x.IsPrivate,
                    TenderDeadline = x.TenderDeadline,
                    TenderQA = x.TenderQA,
                })
                .ToListAsync(ct);
        }

        // -------------------------------------------------
        // GetCalculationDetails
        // -------------------------------------------------
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
                    Compensation = x.Compensation.Name,
                    Contract = x.Contract.Name,
                    Priority = x.Metadata.Priority,
                    Procurement = x.Procurement,
                    ProcurementMethods = x.ProcurementMethods.Name,
                    Name = x.Name,
                    Tax = x.Tax,
                    TimeMonth = x.Metadata.TimeMonth,
                    Type = x.Type.Name,
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
                    HourlyPrice = x.HourlyPriceFactorData.HourlyPrice,
                    Maps = x.Metadata.Maps,
                    Notes = x.Metadata.Notes,
                    Inspector = x.Metadata.Inspector,
                    OverviewInfo = x.Metadata.OverviewInfo,
                    Responsibles = x.Metadata.Responsibles
                })
                .FirstOrDefaultAsync(ct);
        }

        // -------------------------------------------------
        // GetCalculationPost (للنماذج في UI)
        // -------------------------------------------------
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
                    HourlyPrice = x.HourlyPriceFactorData.HourlyPrice,
                    Maps = x.Metadata.Maps,
                    Notes = x.Metadata.Notes,
                    Inspector = x.Metadata.Inspector,
                    OverviewInfo = x.Metadata.OverviewInfo,
                    Responsibles = x.Metadata.Responsibles,
                    Priority = x.Metadata.Priority
                })
                .FirstOrDefaultAsync(ct);
        }

        // -------------------------------------------------
        // GetCalculationPage (الهيد + Tasks + Resources + Offers)
        // -------------------------------------------------
        public async Task<CalculationPageDTO?> GetPageAsync(
            int id,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // 1) Header
            var calculationDto = await context.Calculations
                .AsNoTracking()
                .TagWith("CalculationPage.Header")
                .Where(x =>
                    x.Id == id &&
                    (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId) &&
                    (!x.IsPrivate || x.CreatedBy == userId))
                .Select(x => new CalculationPageDTO
                {
                    Tax = x.Tax,
                    Name = x.Name,
                    OrganisationId = x.OrganisationId,
                    Code = x.Code,
                    TemplateId = x.TemplateId,
                    Factors = x.HourlyPriceFactorData.Factors,
                    QuanityList = x.Metadata.QuanityList,
                    Compensation = x.Compensation != null ? x.Compensation.Name : string.Empty,
                    Customer = x.Organisation != null ? x.Organisation.Name : string.Empty,
                    Contract = x.Contract != null ? x.Contract.Name : string.Empty,
                })
                .FirstOrDefaultAsync(ct);

            if (calculationDto is null)
                return null;

            // 2) Offers (كما عندك لكن مع TagWith)
            var offers = await context.Offers
                .AsNoTracking()
                .TagWith("CalculationPage.Offers")
                .Where(o => o.Resource.Task.CalculationId == id)
                .Select(o => new
                {
                    o.ResourceId,
                    Offer = new ListOfferDTO
                    {
                        Id = o.Id,
                        BaseCost = o.Metadata.BaseCost,
                        Cost = o.Metadata.Cost,
                        Comment = o.Metadata.Comment,
                        Date = o.Date,
                        OrganisationId = o.OrganisationId,
                        Organisation = o.Organisation != null ? o.Organisation.Name : string.Empty,
                        SubCategory = (o.Organisation != null && o.Organisation.OrganisationCategory != null)
                            ? o.Organisation.OrganisationCategory.Name
                            : string.Empty,
                        Category = (o.Organisation != null &&
                                    o.Organisation.OrganisationCategory != null &&
                                    o.Organisation.OrganisationCategory.ParentCategory != null)
                            ? o.Organisation.OrganisationCategory.ParentCategory.Name
                            : string.Empty
                    }
                })
                .ToListAsync(ct);

            var offersByResource = offers
                .GroupBy(o => o.ResourceId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ListOfferDTO>)g.Select(x => x.Offer).ToList());

            // 3) Tasks فقط (بدون Resources)
            var tasks = await context.Tasks
                .AsNoTracking()
                .TagWith("CalculationPage.Tasks")
                .Where(t => t.CalculationId == id)
                .Select(t => new TaskListDTO
                {
                    TaskId = t.ParentTaskId,
                    Id = t.Id,
                    Name = t.Name,
                    OpportunityId = t.OpportunityId,
                    Order = t.SortOrder,
                    StatusId = t.StatusId,
                    Metadata = t.Metadata, // ✅ تحتاجها كاملة
                    Status = t.Status != null ? t.Status.Name : string.Empty,
                    StatusColor = t.Status != null ? t.Status.Color : string.Empty,
                    Opportunity = t.Opportunity != null ? t.Opportunity.OpportunityType : string.Empty,
                    Resources = new List<ResourceListDTO>()
                })
                .ToListAsync(ct);

            if (tasks.Count == 0)
            {
                calculationDto.Tasks = [];
                return calculationDto;
            }

            var taskIds = tasks.Select(t => t.Id).ToList();
            var resources = await context.Resources
                .AsNoTracking()
                .TagWith("CalculationPage.Resources")
                .Where(r => taskIds.Contains(r.TaskId))
                .Select(r => new
                {
                    r.TaskId,
                    Resource = new ResourceListDTO
                    {
                        Name = r.Name,
                        Active = r.IsActive,
                        Id = r.Id,
                        ResType = r.ResType,
                        ResourceSortId = r.ResourceSortId,
                        ResourceTypeId = r.ResourceTypeId,
                        AccountId = r.AccountId,
                        StatusId = r.StatusId,
                        OfferId = r.PrimaryOfferId,
                        Order = r.SortOrder,
                        OpportunityId = r.OpportunityId,
                        Data = r.Metadata, // ✅ تحتاجها كاملة
                        Opportunity = r.Opportunity != null ? r.Opportunity.OpportunityType : string.Empty,
                        StatusColor = r.Status != null ? r.Status.Color : string.Empty,
                        Status = r.Status != null ? r.Status.Name : string.Empty,
                        Sort = r.ResourceSort != null ? r.ResourceSort.Name : string.Empty,
                        ResName = r.ResourceType != null ? r.ResourceType.Name : string.Empty,
                        Account = r.Account != null ? r.Account.Name : string.Empty,
                        AccountCode = r.Account != null ? r.Account.Code : string.Empty,
                        Offers = new List<ListOfferDTO>()
                    }
                })
                .ToListAsync(ct);

            // 4) اربط Resources -> Tasks
            var resourcesByTask = resources
                .GroupBy(x => x.TaskId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Resource).ToList());

            foreach (var t in tasks)
            {
                if (resourcesByTask.TryGetValue(t.Id, out var list))
                    t.Resources = list;
                else
                    t.Resources = [];
            }

            // 5) اربط Offers -> Resources
            foreach (var t in tasks)
            {
                foreach (var r in t.Resources)
                {
                    if (offersByResource.TryGetValue(r.Id, out var list))
                        r.Offers = list.ToList(); // DTO expects List
                }
            }

            calculationDto.Tasks = tasks;
            return calculationDto;
        }

        // -------------------------------------------------
        // HourlyPriceList (قراءة)
        // -------------------------------------------------
        public async Task<List<HourlyPriceListGroupDTO>> GetHourlyPriceListAsync(
            int id,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var priceList = await context.Calculations
                .AsNoTracking()
                .Where(x => x.Id == id &&
                            (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId))
                .Select(x => x.HourlyPriceFactorData.HourlyPrice)
                .FirstOrDefaultAsync(ct);

            return priceList ?? [];
        }
    }
}
