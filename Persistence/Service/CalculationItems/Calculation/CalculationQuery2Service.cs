using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using ProjectManagement.Shared.DTO.Offer;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Persistence.Service.CalculationItems.Calculation
{
    public sealed partial class CalculationQueryService
    {
        private sealed class CalculationPageHeaderRow
        {
            public double Tax { get; set; }
            public string Name { get; set; } = string.Empty;
            public int? OrganisationId { get; set; }
            public string Code { get; set; } = string.Empty;
            public int? TemplateId { get; set; }
            public int? TemplateColumnId { get; set; }
            public SortConfig Sort { get; set; } = new();
            public List<OHFactors> Factors { get; set; } = [];
            public List<QuanityListDTO> QuanityList { get; set; } = [];
            public string Compensation { get; set; } = string.Empty;
            public string Customer { get; set; } = string.Empty;
            public string Contract { get; set; } = string.Empty;
            public DisplayOptionsPresetStore DisplayPresets { get; set; } = new();
        }

        private sealed class CalculationPageResourceRow
        {
            public int Id { get; set; }
            public int TaskId { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public ProjectManagement.Shared.Enums.ResourceTypesEnum ResType { get; set; }
            public int? ResourceSortId { get; set; }
            public int? ResourceTypeId { get; set; }
            public int? AccountId { get; set; }
            public int? StatusId { get; set; }
            public int? PrimaryOfferId { get; set; }
            public int SortOrder { get; set; }
            public int? OpportunityId { get; set; }
            public string Note { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public decimal? Quantity { get; set; }
            public string Opportunity { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string StatusColor { get; set; } = string.Empty;
            public string Sort { get; set; } = string.Empty;
            public string ResName { get; set; } = string.Empty;
            public string Account { get; set; } = string.Empty;
            public string AccountCode { get; set; } = string.Empty;
            public ProjectManagement.Shared.Base.Calculation.ResourceMetadata Metadata { get; set; } = new();
        }

        private sealed class CalculationPageOfferRow
        {
            public int Id { get; set; }
            public int ResourceId { get; set; }
            public decimal BaseCost { get; set; }
            public decimal Cost { get; set; }
            public string Comment { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public int? OrganisationId { get; set; }
            public string Organisation { get; set; } = string.Empty;
            public string SubCategory { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
        }

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
                .Select(x => new CalculationPageHeaderRow
                {
                    Tax = x.Tax,
                    Name = x.Name,
                    OrganisationId = x.OrganisationId,
                    Code = x.Code,
                    TemplateId = x.TemplateId,
                    TemplateColumnId = x.TemplateColumnId,
                    Sort = x.Sort,
                    Factors = x.Factors,
                    QuanityList = x.Metadata.QuanityList,
                    Compensation = x.Compensation == null ? string.Empty : (x.Compensation.Name ?? string.Empty),
                    Customer = x.Organisation == null ? string.Empty : (x.Organisation.Name ?? string.Empty),
                    Contract = x.Contract == null ? string.Empty : (x.Contract.Name ?? string.Empty),
                    DisplayPresets = x.DisplayPresets,
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
                    SortOrder = t.SortOrder,
                    Quantity = t.Quantity,
                    Unit = t.Unit ?? string.Empty,
                    StatusId = t.StatusId,
                    Status = t.Status == null ? string.Empty : (t.Status.Name ?? string.Empty),
                    StatusColor = t.Status == null ? string.Empty : (t.Status.Color ?? string.Empty),
                    OpportunityId = t.OpportunityId,
                    Opportunity = t.Opportunity == null ? string.Empty : (t.Opportunity.OpportunityType ?? string.Empty),
                    Metadata = CalculationItemMetadataMapper.BuildTaskMetadata(
                        t.Metadata,
                        t.Note,
                        null,
                        t.Code,
                        t.IsActive,
                        t.Type,
                        t.IsOH),
                })
                .ToListAsync(ct);

            if (taskDtos.Count == 0)
                return BuildHeaderDto(header, []);

            var tasksById = taskDtos.ToDictionary(t => t.Id);
            var taskIds = taskDtos.Select(t => t.Id).ToList();

            // 3) Resources (one row per resource)
            var resourceRows = await context.Resources
                .AsNoTracking()
                .TagWith("CalcPageOptimizedV2.Resources")
                .Where(r => taskIds.Contains(r.TaskId))
                .OrderBy(r => r.TaskId)
                .ThenBy(r => r.SortOrder)
                .Select(r => new CalculationPageResourceRow
                {
                    Id = r.Id,
                    TaskId = r.TaskId,
                    Name = r.Name,
                    IsActive = r.IsActive,
                    ResType = r.ResType,
                    ResourceSortId = r.ResourceSortId,
                    ResourceTypeId = r.ResourceTypeId,
                    AccountId = r.AccountId,
                    StatusId = r.StatusId,
                    PrimaryOfferId = r.PrimaryOfferId,
                    SortOrder = r.SortOrder,
                    OpportunityId = r.OpportunityId,
                    Note = r.Note ?? string.Empty,
                    Unit = r.Unit ?? string.Empty,
                    Quantity = r.Quantity,

                    Opportunity = r.Opportunity == null ? string.Empty : (r.Opportunity.OpportunityType ?? string.Empty),
                    Status = r.Status == null ? string.Empty : (r.Status.Name ?? string.Empty),
                    StatusColor = r.Status == null ? string.Empty : (r.Status.Color ?? string.Empty),

                    Sort = r.ResourceSort == null ? string.Empty : (r.ResourceSort.Name ?? string.Empty),
                    ResName = r.ResourceType == null ? string.Empty : (r.ResourceType.Name ?? string.Empty),

                    Account = r.Account == null ? string.Empty : (r.Account.Name ?? string.Empty),
                    AccountCode = r.Account == null ? string.Empty : (r.Account.Code ?? string.Empty),

                    Metadata = r.Metadata
                })
                .ToListAsync(ct);

            var resourcesById = new Dictionary<int, ResourceListDTO>(capacity: resourceRows.Count);

            foreach (var r in resourceRows)
            {
                if (!tasksById.TryGetValue(r.TaskId, out var taskDto))
                    continue;

                var resDto = MapResourceRow(r);

                resourcesById[r.Id] = resDto;
                taskDto.Resources.Add(resDto);
            }

            if (resourcesById.Count == 0)
                return BuildHeaderDto(header, taskDtos);

            var resourceIds = resourcesById.Keys.ToList();

            // 4) Offers (one row per offer)
            var offerRows = await context.Offers
                .AsNoTracking()
                .TagWith("CalcPageOptimizedV2.Offers")
                .Where(o => resourceIds.Contains(o.ResourceId))
                .OrderBy(o => o.ResourceId)
                .ThenByDescending(o => o.Date)
                .Select(o => new CalculationPageOfferRow
                {
                    Id = o.Id,
                    ResourceId = o.ResourceId,
                    BaseCost = o.Metadata.BaseCost,
                    Cost = o.Metadata.Cost,
                    Comment = o.Comment ?? string.Empty,
                    Date = o.Date,
                    OrganisationId = o.OrganisationId,
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

                resDto.Offers.Add(MapOfferRow(o));
            }

            return BuildHeaderDto(header, taskDtos);
        }

        private static CalculationPageDTO BuildHeaderDto(
            CalculationPageHeaderRow header,
            List<TaskListDTO> tasks)
        {
            return new CalculationPageDTO
            {
                Tax = header.Tax,
                Name = header.Name,
                OrganisationId = header.OrganisationId,
                Code = header.Code,
                TemplateId = header.TemplateId,
                TemplateColumnId = header.TemplateColumnId,
                Sort = header.Sort,
                Factors = header.Factors ?? [],
                QuanityList = header.QuanityList ?? [],
                Compensation = header.Compensation ?? string.Empty,
                Customer = header.Customer ?? string.Empty,
                Contract = header.Contract ?? string.Empty,
                DisplayPresets = DisplayOptionsPresetState.Normalize(header.DisplayPresets),
                Tasks = tasks
            };
        }

        private static ResourceListDTO MapResourceRow(CalculationPageResourceRow row)
        {
            return new ResourceListDTO
            {
                Id = row.Id,
                TaskId = row.TaskId,
                Name = row.Name ?? string.Empty,
                IsActive = row.IsActive,
                ResType = row.ResType,
                ResourceSortId = row.ResourceSortId,
                ResourceTypeId = row.ResourceTypeId,
                AccountId = row.AccountId,
                StatusId = row.StatusId,
                OfferId = row.PrimaryOfferId,
                SortOrder = row.SortOrder,
                OpportunityId = row.OpportunityId,
                Opportunity = row.Opportunity ?? string.Empty,
                Status = row.Status ?? string.Empty,
                StatusColor = row.StatusColor ?? string.Empty,
                Sort = row.Sort ?? string.Empty,
                ResName = row.ResName ?? string.Empty,
                Account = row.Account ?? string.Empty,
                AccountCode = row.AccountCode ?? string.Empty,
                Data = CalculationItemMetadataMapper.BuildResourceMetadata(
                    row.Metadata,
                    row.Note,
                    null),
                Quantity = row.Quantity,
                Unit = row.Unit,
                Offers = []
            };
        }

        private static ListOfferDTO MapOfferRow(CalculationPageOfferRow row)
        {
            return new ListOfferDTO
            {
                Id = row.Id,
                BaseCost = row.BaseCost,
                Cost = row.Cost,
                Comment = row.Comment ?? string.Empty,
                Date = row.Date,
                OrganisationId = row.OrganisationId,
                Organisation = row.Organisation ?? string.Empty,
                SubCategory = row.SubCategory ?? string.Empty,
                Category = row.Category ?? string.Empty
            };
        }
    }
}
