namespace Persistence.Service.CalculationItems
{
    using Application.Feature.Calculation.Calculation;
    using Application.Interfaces.Context;
    using Microsoft.EntityFrameworkCore;
    using ProjectManagement.Shared.DTO.Calculation;
    using ProjectManagement.Shared.DTO.Offer;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class CalculationQueryService(IShardingSingleDbContext context) : ICalculationQueryService
    {

        // -------------------------------------------------
        // GetAllCalculations (بنفس منطق GetAllCalculationsQuery)
        // -------------------------------------------------
        public async Task<IEnumerable<ListCalculationDTO>> GetAllAsync(
            Guid projectId,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default)
        {
            return await context.Calculations
                .AsNoTracking()
                .Where(x =>
                    x.ProjectId == projectId &&
                    (departmentId == null || x.Project.Folder.DepartmentId == departmentId) &&
                    (!x.IsPrivate || x.CreatedBy == userId))
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
                .ToListAsync(cancellationToken);
        }

        // -------------------------------------------------
        // GetAllCalculationsByDepartment
        // -------------------------------------------------
        public async Task<IEnumerable<ListCalculationDTO>> GetByDepartmentAsync(
            Guid projectId,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default)
        {
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
                .ToListAsync(cancellationToken);
        }

        // -------------------------------------------------
        // GetCalculationDetails
        // -------------------------------------------------
        public async Task<CalculationDetailsDTO?> GetDetailsAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
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
                .FirstOrDefaultAsync(cancellationToken);
        }

        // -------------------------------------------------
        // GetCalculationPost (للنماذج في UI)
        // -------------------------------------------------
        public async Task<CalculationPostDTO?> GetPostModelAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
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
                .FirstOrDefaultAsync(cancellationToken);
        }

        // -------------------------------------------------
        // GetCalculationPage (الهيد + Tasks + Resources + Offers)
        // -------------------------------------------------
        public async Task<CalculationPageDTO?> GetPageAsync(
            int id,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default)
        {
            var calculationDto = await context.Calculations
                .AsNoTracking()
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
                .FirstOrDefaultAsync(cancellationToken);

            if (calculationDto is null)
                return null;

            // العروض
            var offers = await context.Offers
                .AsNoTracking()
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
                .ToListAsync(cancellationToken);

            var offersByResource = offers
                .GroupBy(o => o.ResourceId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Offer).ToList());

            // المهام + الموارد (كما في GetCalculationPageQuery)
            var tasks = await context.Tasks
                .AsNoTracking()
                .Where(t => t.CalculationId == id)
                .Select(t => new TaskListDTO
                {
                    TaskId = t.ParentTaskId,
                    Id = t.Id,
                    Name = t.Name,
                    OpportunityId = t.OpportunityId,
                    Order = t.SortOrder,
                    StatusId = t.StatusId,
                    Data = t.Metadata,
                    Status = t.Status != null ? t.Status.Name : string.Empty,
                    StatusColor = t.Status != null ? t.Status.Color : string.Empty,
                    Opportunity = t.Opportunity != null ? t.Opportunity.OpportunityType : string.Empty,
                    Resources = t.Resources.Select(r => new ResourceListDTO
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
                        Data = r.Metadata,
                        Opportunity = r.Opportunity != null ? r.Opportunity.OpportunityType : string.Empty,
                        StatusColor = r.Status != null ? r.Status.Color : string.Empty,
                        Status = r.Status != null ? r.Status.Name : string.Empty,
                        Sort = r.ResourceSort != null ? r.ResourceSort.Name : string.Empty,
                        ResName = r.ResourceType != null ? r.ResourceType.Name : string.Empty,
                        Account = r.Account != null ? r.Account.Name : string.Empty,
                        AccountCode = r.Account != null ? r.Account.Code : string.Empty,
                        Offers = new List<ListOfferDTO>()
                    }).ToList()
                })
                .ToListAsync(cancellationToken);

            // ربط Offers بالـ Resources
            foreach (var t in tasks)
            {
                foreach (var r in t.Resources)
                {
                    if (offersByResource.TryGetValue(r.Id, out var list))
                        r.Offers = list;
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
            CancellationToken cancellationToken = default)
        {
            var priceList = await context.Calculations
                .AsNoTracking()
                .Where(x => x.Id == id &&
                            (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId))
                .Select(x => x.HourlyPriceFactorData.HourlyPrice)
                .FirstOrDefaultAsync(cancellationToken);

            return priceList ?? [];
        }
    }
}
