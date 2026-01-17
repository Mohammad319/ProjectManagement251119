using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Offer;

namespace Persistence.Service.CalculationItems.Calculation
{
    public sealed partial class CalculationQueryService
    {
        public async Task<CalculationPageDTO?> GetPageAsync(
            int id,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // 1) Header (same auth logic)
            var header = await context.Calculations
                .AsNoTracking()
                .TagWith("CalcPageOptimized.Header")
                .Where(x =>
                    x.Id == id &&
                    (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId.Value) &&
                    (!x.IsPrivate || x.CreatedBy == userId))
                .Select(x => new CalculationPageDTO
                {
                    Tax = x.Tax,
                    Name = x.Name,
                    OrganisationId = x.OrganisationId,
                    Code = x.Code,
                    TemplateId = x.TemplateId,
                    Factors = x.Factors,
                    QuanityList = x.Metadata.QuanityList,
                    Compensation = x.Compensation == null ? string.Empty : x.Compensation.Name,
                    Customer = x.Organisation == null ? string.Empty : x.Organisation.Name,
                    Contract = x.Contract == null ? string.Empty : x.Contract.Name,
                })
                .FirstOrDefaultAsync(ct);

            if (header is null)
                return null;

            // 2) Flat query WITHOUT heavy metadata (TaskMetadata/ResourceMetadata are text-heavy)
            // NOTE: Avoid ?. inside expression trees; use ternary/== null pattern for EF translation.
            var rows = await (
                from t in context.Tasks.AsNoTracking().TagWith("CalcPageOptimized.Flat.NoMetadata")
                where t.CalculationId == id

                join r in context.Resources.AsNoTracking()
                    on t.Id equals r.TaskId into rg
                from r in rg.DefaultIfEmpty()

                join o in context.Offers.AsNoTracking()
                    on (int?)r.Id equals (int?)o.ResourceId into og
                from o in og.DefaultIfEmpty()

                select new CalcPageFlatRow
                {
                    // Task
                    TaskId = t.Id,
                    ParentTaskId = t.ParentTaskId,
                    TaskName = t.Name,
                    TaskOrder = t.SortOrder,
                    TaskStatusId = t.StatusId,
                    TaskStatusName = t.Status == null ? string.Empty : (t.Status.Name ?? string.Empty),
                    TaskStatusColor = t.Status == null ? string.Empty : (t.Status.Color ?? string.Empty),
                    TaskOpportunityId = t.OpportunityId,
                    TaskOpportunity = t.Opportunity == null ? string.Empty : (t.Opportunity.OpportunityType ?? string.Empty),
                    TaskMetadata = default!, // loaded later once per task

                    // Resource (nullable)
                    ResourceId = (int?)r.Id,
                    ResourceName = r.Name,
                    ResourceIsActive = r.IsActive,
                    ResType = r.ResType,
                    ResourceSortId = r.ResourceSortId,
                    ResourceTypeId = r.ResourceTypeId,
                    ResourceAccountId = r.AccountId,
                    ResourceStatusId = r.StatusId,
                    ResourcePrimaryOfferId = r.PrimaryOfferId,
                    ResourceOrder = r.SortOrder,
                    ResourceOpportunityId = r.OpportunityId,

                    ResourceOpportunity =
                        r == null
                            ? string.Empty
                            : (r.Opportunity == null ? string.Empty : (r.Opportunity.OpportunityType ?? string.Empty)),

                    ResourceStatusColor =
                        r == null
                            ? string.Empty
                            : (r.Status == null ? string.Empty : (r.Status.Color ?? string.Empty)),

                    ResourceStatus =
                        r == null
                            ? string.Empty
                            : (r.Status == null ? string.Empty : (r.Status.Name ?? string.Empty)),

                    ResourceSort =
                        r == null
                            ? string.Empty
                            : (r.ResourceSort == null ? string.Empty : (r.ResourceSort.Name ?? string.Empty)),

                    ResourceTypeName =
                        r == null
                            ? string.Empty
                            : (r.ResourceType == null ? string.Empty : (r.ResourceType.Name ?? string.Empty)),

                    ResourceAccount =
                        r == null
                            ? string.Empty
                            : (r.Account == null ? string.Empty : (r.Account.Name ?? string.Empty)),

                    ResourceAccountCode =
                        r == null
                            ? string.Empty
                            : (r.Account == null ? string.Empty : (r.Account.Code ?? string.Empty)),

                    ResourceMetadata = default!, // loaded later once per resource

                    // Offer (nullable) - keep costs
                    OfferId = (int?)o.Id,
                    OfferBaseCost = o == null ? 0 : o.Metadata.BaseCost,
                    OfferCost = o == null ? 0 : o.Metadata.Cost,
                    OfferComment = o.Metadata.Comment,
                    OfferDate = o.Date,
                    OfferOrganisationId = o.OrganisationId,

                    OfferOrganisation =
                        o == null
                            ? string.Empty
                            : (o.Organisation == null ? string.Empty : (o.Organisation.Name ?? string.Empty)),

                    OfferSubCategory =
                        o == null
                            ? string.Empty
                            : (o.Organisation == null
                                ? string.Empty
                                : (o.Organisation.OrganisationCategory == null
                                    ? string.Empty
                                    : (o.Organisation.OrganisationCategory.Name ?? string.Empty))),

                    OfferCategory =
                        o == null
                            ? string.Empty
                            : (o.Organisation == null
                                ? string.Empty
                                : (o.Organisation.OrganisationCategory == null
                                    ? string.Empty
                                    : (o.Organisation.OrganisationCategory.ParentCategory == null
                                        ? string.Empty
                                        : (o.Organisation.OrganisationCategory.ParentCategory.Name ?? string.Empty)))),
                }
            ).ToListAsync(ct);

            // 3) Grouping: build TaskListDTO -> ResourceListDTO -> Offers
            var tasks = new Dictionary<int, TaskListDTO>(capacity: 2048);
            var resourceMaps = new Dictionary<int, Dictionary<int, ResourceListDTO>>(capacity: 2048);

            foreach (var row in rows)
            {
                if (!tasks.TryGetValue(row.TaskId, out var taskDto))
                {
                    taskDto = new TaskListDTO
                    {
                        TaskId = row.ParentTaskId,
                        Id = row.TaskId,
                        Name = row.TaskName,
                        OpportunityId = row.TaskOpportunityId,
                        Order = row.TaskOrder,
                        StatusId = row.TaskStatusId,

                        // initialized; later overwritten by loaded metadata
                        Metadata = new TaskMetadata(),

                        Status = row.TaskStatusName,
                        StatusColor = row.TaskStatusColor,
                        Opportunity = row.TaskOpportunity,
                        Resources = []
                    };

                    tasks[row.TaskId] = taskDto;
                    resourceMaps[row.TaskId] = new(capacity: 8);
                }

                // Task without resources
                if (!row.ResourceId.HasValue)
                    continue;

                var resId = row.ResourceId.Value;
                var resMap = resourceMaps[row.TaskId];

                if (!resMap.TryGetValue(resId, out var resDto))
                {
                    resDto = new ResourceListDTO
                    {
                        Id = resId,
                        Name = row.ResourceName ?? string.Empty,
                        Active = row.ResourceIsActive ?? false,
                        ResType = row.ResType ?? default,
                        ResourceSortId = row.ResourceSortId,
                        ResourceTypeId = row.ResourceTypeId,
                        AccountId = row.ResourceAccountId,
                        StatusId = row.ResourceStatusId,
                        OfferId = row.ResourcePrimaryOfferId,
                        Order = row.ResourceOrder ?? 0,
                        OpportunityId = row.ResourceOpportunityId,

                        // initialized; later overwritten by loaded metadata
                        Data = new ResourceMetadata(),

                        Opportunity = row.ResourceOpportunity,
                        StatusColor = row.ResourceStatusColor,
                        Status = row.ResourceStatus,
                        Sort = row.ResourceSort,
                        ResName = row.ResourceTypeName,
                        Account = row.ResourceAccount,
                        AccountCode = row.ResourceAccountCode,
                        Offers = []
                    };

                    resMap[resId] = resDto;
                    taskDto.Resources.Add(resDto);
                }

                // Offer (if exists)
                if (row.OfferId.HasValue)
                {
                    resDto.Offers.Add(new ListOfferDTO
                    {
                        Id = row.OfferId.Value,
                        BaseCost = row.OfferBaseCost,
                        Cost = row.OfferCost,
                        Comment = row.OfferComment,
                        Date = row.OfferDate ?? default,
                        OrganisationId = row.OfferOrganisationId ?? 0,
                        Organisation = row.OfferOrganisation,
                        SubCategory = row.OfferSubCategory,
                        Category = row.OfferCategory
                    });
                }
            }

            // 4) Load heavy metadata ONCE per Task and ONCE per Resource, then inject into DTOs
            var taskIds = tasks.Keys.ToArray();

            var allResourceDtos = tasks.Values.SelectMany(t => t.Resources).ToList();
            var resourceIds = allResourceDtos.Select(r => r.Id).Distinct().ToArray();

            // Task metadata (one row per task)
            if (taskIds.Length > 0)
            {
                var taskMeta = await context.Tasks
                    .AsNoTracking()
                    .TagWith("CalcPageOptimized.TaskMetadata")
                    .Where(t => t.CalculationId == id && taskIds.Contains(t.Id))
                    .Select(t => new { t.Id, t.Metadata })
                    .ToDictionaryAsync(x => x.Id, x => x.Metadata, ct);

                foreach (var t in tasks.Values)
                {
                    if (taskMeta.TryGetValue(t.Id, out var meta))
                        t.Metadata = meta;
                }
            }

            // Resource metadata (one row per resource)
            if (resourceIds.Length > 0)
            {
                var resMeta = await context.Resources
                    .AsNoTracking()
                    .TagWith("CalcPageOptimized.ResourceMetadata")
                    .Where(r => resourceIds.Contains(r.Id))
                    .Select(r => new { r.Id, r.Metadata })
                    .ToDictionaryAsync(x => x.Id, x => x.Metadata, ct);

                foreach (var r in allResourceDtos)
                {
                    if (resMeta.TryGetValue(r.Id, out var meta))
                        r.Data = meta;
                }
            }

            header.Tasks = [.. tasks.Values.OrderBy(t => t.Order)];

            return header;
        }

    }
}
