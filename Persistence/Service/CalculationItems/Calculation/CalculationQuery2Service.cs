using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Offer;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

            // 1) Header (authorization gate)
            var header = await context.Calculations
                .AsNoTracking()
                .TagWith("CalcPageOptimizedV2.Header")
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
                    Compensation = x.Compensation == null ? string.Empty : (x.Compensation.Name ?? string.Empty),
                    Customer = x.Organisation == null ? string.Empty : (x.Organisation.Name ?? string.Empty),
                    Contract = x.Contract == null ? string.Empty : (x.Contract.Name ?? string.Empty),
                })
                .FirstOrDefaultAsync(ct);

            if (header is null)
                return null;

            // 2) Tasks (one row per task)
            var taskDtos = await context.Tasks
                .AsNoTracking()
                .TagWith("CalcPageOptimizedV2.Tasks")
                .Where(t => t.CalculationId == id)
                .OrderBy(t => t.SortOrder)
                .Select(t => new TaskListDTO
                {
                    Id = t.Id,
                    TaskId = t.ParentTaskId,
                    Name = t.Name,
                    Order = t.SortOrder,
                    StatusId = t.StatusId,
                    Status = t.Status == null ? string.Empty : (t.Status.Name ?? string.Empty),
                    StatusColor = t.Status == null ? string.Empty : (t.Status.Color ?? string.Empty),
                    OpportunityId = t.OpportunityId,
                    Opportunity = t.Opportunity == null ? string.Empty : (t.Opportunity.OpportunityType ?? string.Empty),

                    // TaskMetadata: safe here (one row per task)
                    Metadata = t.Metadata,
                })
                .ToListAsync(ct);

            if (taskDtos.Count == 0)
                return header;

            var tasksById = taskDtos.ToDictionary(t => t.Id);
            var taskIds = taskDtos.Select(t => t.Id).ToList();

            // 3) Resources (one row per resource)
            var resourceRows = await context.Resources
                .AsNoTracking()
                .TagWith("CalcPageOptimizedV2.Resources")
                .Where(r => taskIds.Contains(r.TaskId))
                .OrderBy(r => r.TaskId)
                .ThenBy(r => r.SortOrder)
                .Select(r => new
                {
                    r.Id,
                    r.TaskId,
                    r.Name,
                    r.IsActive,
                    r.ResType,
                    r.ResourceSortId,
                    r.ResourceTypeId,
                    r.AccountId,
                    r.StatusId,
                    r.PrimaryOfferId,
                    r.SortOrder,
                    r.OpportunityId,

                    Opportunity = r.Opportunity == null ? string.Empty : (r.Opportunity.OpportunityType ?? string.Empty),
                    Status = r.Status == null ? string.Empty : (r.Status.Name ?? string.Empty),
                    StatusColor = r.Status == null ? string.Empty : (r.Status.Color ?? string.Empty),

                    Sort = r.ResourceSort == null ? string.Empty : (r.ResourceSort.Name ?? string.Empty),
                    ResName = r.ResourceType == null ? string.Empty : (r.ResourceType.Name ?? string.Empty),

                    Account = r.Account == null ? string.Empty : (r.Account.Name ?? string.Empty),
                    AccountCode = r.Account == null ? string.Empty : (r.Account.Code ?? string.Empty),

                    // ResourceMetadata: safe here (one row per resource)
                    Metadata = r.Metadata
                })
                .ToListAsync(ct);

            var resourcesById = new Dictionary<int, ResourceListDTO>(capacity: resourceRows.Count);

            foreach (var r in resourceRows)
            {
                if (!tasksById.TryGetValue(r.TaskId, out var taskDto))
                    continue;

                var resDto = new ResourceListDTO
                {
                    Id = r.Id,
                    TaskId = r.TaskId,
                    Name = r.Name ?? string.Empty,
                    Active = r.IsActive,
                    ResType = r.ResType,
                    ResourceSortId = r.ResourceSortId,
                    ResourceTypeId = r.ResourceTypeId,
                    AccountId = r.AccountId,
                    StatusId = r.StatusId,
                    OfferId = r.PrimaryOfferId,
                    Order = r.SortOrder,
                    OpportunityId = r.OpportunityId,

                    Opportunity = r.Opportunity ?? string.Empty,
                    Status = r.Status ?? string.Empty,
                    StatusColor = r.StatusColor ?? string.Empty,
                    Sort = r.Sort ?? string.Empty,
                    ResName = r.ResName ?? string.Empty,
                    Account = r.Account ?? string.Empty,
                    AccountCode = r.AccountCode ?? string.Empty,

                    Data = r.Metadata,
                    Offers = []
                };

                resourcesById[r.Id] = resDto;
                taskDto.Resources.Add(resDto);
            }

            if (resourcesById.Count == 0)
            {
                header.Tasks = taskDtos;
                return header;
            }

            var resourceIds = resourcesById.Keys.ToList();

            // 4) Offers (one row per offer)
            var offerRows = await context.Offers
                .AsNoTracking()
                .TagWith("CalcPageOptimizedV2.Offers")
                .Where(o => resourceIds.Contains(o.ResourceId))
                .OrderBy(o => o.ResourceId)
                .ThenByDescending(o => o.Date)
                .Select(o => new
                {
                    o.Id,
                    o.ResourceId,
                    BaseCost = o.Metadata.BaseCost,
                    Cost = o.Metadata.Cost,
                    Comment = o.Metadata.Comment,
                    o.Date,
                    o.OrganisationId,
                    Organisation = o.Organisation == null ? string.Empty : (o.Organisation.Name ?? string.Empty),
                    SubCategory = o.Organisation == null
                        ? string.Empty
                        : (o.Organisation.OrganisationCategory == null
                            ? string.Empty
                            : (o.Organisation.OrganisationCategory.Name ?? string.Empty)),
                    Category = o.Organisation == null
                        ? string.Empty
                        : (o.Organisation.OrganisationCategory == null
                            ? string.Empty
                            : (o.Organisation.OrganisationCategory.ParentCategory == null
                                ? string.Empty
                                : (o.Organisation.OrganisationCategory.ParentCategory!.Name ?? string.Empty))),
                })
                .ToListAsync(ct);

            foreach (var o in offerRows)
            {
                if (!resourcesById.TryGetValue(o.ResourceId, out var resDto))
                    continue;

                resDto.Offers.Add(new ListOfferDTO
                {
                    Id = o.Id,
                    BaseCost = o.BaseCost,
                    Cost = o.Cost,
                    Comment = o.Comment ?? string.Empty,
                    Date = o.Date,
                    OrganisationId = o.OrganisationId,
                    Organisation = o.Organisation ?? string.Empty,
                    SubCategory = o.SubCategory ?? string.Empty,
                    Category = o.Category ?? string.Empty,
                });
            }

            header.Tasks = taskDtos;
            return header;
        }
    }
}
