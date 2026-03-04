using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Entities.Tasks;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
namespace ProjectManagement.Shared.Mappers;

public static class ProjectTaskProjection
{
    public static IQueryable<ProjectTaskDto> TasksBaseToDto(
    this IQueryable<TaskDefinition> query, int tenantid)
    {
        return query.AsNoTracking().AsSplitQuery()
            .Select(t => new ProjectTaskDto
            {
                Id = t.Id,
                DisplayName = t.Name,
                SortOrder = t.SortOrder,
                IsVisible = t.IsVisible,
                CapacityResourceId = t.CapacityResourceId,
                UnitGroupId = t.TaskUnitGroupId,
                Code = t.Code,
                NewUnitCode = t.NewUnitCode,
                Note = t.FieldNotes,
                UnitCode = t.UnitCode,
                WorkloadThresholds = t.WorkloadThresholds,
                Quantity = t.Quantity,
                Uncontrollable = t.Uncontrollable,
                UnitGroup = t.TaskUnitGroup == null ? null : new UnitGroupDto
                {
                    Id = t.TaskUnitGroup.Id,
                    DisplayName = t.TaskUnitGroup.DisplayName,
                    Keys = t.TaskUnitGroup.Keys,
                },

                BaseResources = t.TaskResourceAssignments.Select(a => new
                    {
                        a,
                        Link = a.Resource != null
                            ? a.Resource.TenantLinks
                                .Where(x => x.TenantId == tenantid)
                                .Select(x => new
                                {
                                    x.AccountId,
                                    x.Name,
                                    x.ResourceTypeId,
                                    x.ResourceSortId,
                                    x.StatusId,
                                    x.Co2,
                                    x.Cost,
                                })
                                .FirstOrDefault()
                            : null // APPLY/LEFT JOIN واحد فقط
                    })
                    .Select(z => new ResourceDto
                    {
                        Id = z.a.ResourceId,
                        Name = z.a.Resource != null ? z.a.Resource.Name : string.Empty,
                        CalcResCost = z.a.Resource.CalcResCost ?? new (),
                        Active = z.a.IsActive,
                        FolderId = z.a.Resource.FolderId,
                        ResType = z.a.Resource.ResType,
                        SortOrder = z.a.Resource.SortOrder,
                        CostRole = z.a.CapacityRoles,
                        CostStorageValue = (double?)z.a.Resource.Data.Cost,
                        CostUserValue = z.Link == null ? null : z.Link.Cost,
                        NameUserValue = z.Link == null || z.Link.Name == null ? string.Empty : z.Link.Name,

                        AccountId = z.Link == null ? null : z.Link.AccountId,
                        ResourceTypeId = z.Link == null ? (int?)null : z.Link.ResourceTypeId,
                        ResourceSortId = z.Link == null ? (int?)null : z.Link.ResourceSortId,
                        StatusId = z.Link == null ? (int?)null : z.Link.StatusId,
                        MenuId = z.a.MenuId,
                        Properties = z.a.Resource.AttributeValues
                            .Select(b => new ResourcePropertyBindDto
                            {
                                Id = b.AttributeId,
                                NumberDefault = b.NumericValue,
                                DataType = b.Attribute != null ? b.Attribute.DataType : default,
                                DisplayName = b.Attribute != null ? b.Attribute.DisplayName : string.Empty,
                                IsUserEditable = b.Attribute != null && b.Attribute.IsUserEditable,
                                MaxNumericValue = b.Attribute != null ? b.Attribute.MaxNumericValue : null,
                                TextDefault = b.TextValue,
                            }).ToList(),

                        Data = new ResourceMetadata
                        {
                            ChangeFactor1 = z.a.ChangeFactor1,
                            ChangeFactor2 = z.a.ChangeFactor2,
                            CapWaste = z.a.CapWaste,
                            BaseCost = (decimal?)z.a.BaseCost,
                            CO2 = z.Link == null ? null : z.Link.Co2,
                            Cost = z.a.Resource.Data.Cost,
                            Note = z.a.Resource.Data.Note,
                            UpperNote = z.a.Resource.Data.UpperNote,
                            Quantity = z.a.Resource.Data.Quantity,
                            Unit = z.a.Resource.Data.Unit,
                            QuantityParam = z.a.Resource.Data.QuantityParam,
                        }
                    })
                    .ToList(),
            });
    }

   public static IQueryable<ProjectTaskDto> ProjectToDto(
    this IQueryable<TaskDefinition> query, int tenantid, int depid)
    {
        return query.AsNoTracking().AsSplitQuery()
            .Select(t => new ProjectTaskDto
            {
                Id = t.Id,
                DisplayName = t.Name,
                SortOrder = t.SortOrder,
                IsVisible = t.IsVisible,
                CapacityResourceId = t.CapacityResourceId,
                UnitGroupId = t.TaskUnitGroupId,
                Code = t.Code,
                NewUnitCode = t.NewUnitCode,
                Note = t.FieldNotes,
                UnitCode = t.UnitCode,
                WorkloadThresholds = t.WorkloadThresholds,
                Quantity = t.Quantity,
                Uncontrollable = t.Uncontrollable,
                UpperNote = t.RowNotes,
               
                UnitGroup = t.TaskUnitGroup == null ? null : new UnitGroupDto
                {
                    Id = t.TaskUnitGroup.Id,
                    DisplayName = t.TaskUnitGroup.DisplayName,
                    Keys = t.TaskUnitGroup.Keys,
                },
                OptionGroups = t.QuestionGroups.Select(g => new OptionGroupDto
                {
                    Id = g.Id,
                    DisplayName = g.DisplayName,
                    SectionKey = g.SectionKey,
                    SelectionMode = g.SelectionMode,
                    SortOrder = g.SortOrder,
                    TaskId = g.TaskId,
                    Options = g.Options.Select(o => new OptionItemDto
                    {
                        Id = o.Id,
                        DisplayName = o.DisplayName,
                        RevealedSectionKeys = o.RevealedSectionKeys,
                        OptionGroupId = o.QuestionGroupId,
                    }).ToList()
                }).ToList(),
                ResourceOptionGroups = t.ResourceSelectors.Select(g => new ResourceOptionGroupDto
                {
                    Id = g.Id,
                    DisplayName = g.DisplayName,
                    SectionKey = g.SectionKey,
                    SortOrder = g.SortOrder,
                    Items = g.Items.Select(i => new ResourceOptionItemDto
                    {
                        Id = i.Id,
                        ResourceName = i.Resource.Name,
                    }).ToList()
                }).ToList(),
                NumericInputs = t.NumericQuestions.Select(n => new NumericInputDto
                {
                    Id = n.Id,
                    DisplayName = n.DisplayName,
                    SortOrder = n.SortOrder,
                    //ParentTaskId = n.ParentTaskId,
                    MaxInputValue = n.MaxInputValue,
                    MinInputValue = n.MinInputValue,
                    SectionKey = n.SectionKey,
                }).ToList(),
                Conditions = t.Conditions.Select(c => new TaskConditionDto
                {
                    Id = c.Id,
                    NumericToResourceLogic = c.NumericResourceLogic,
                    OptionToNumericLogic = c.OptionNumericLogic,
                    OptionToResourceLogic = c.OptionResourceLogic,

                    VariableRequirements = c.VariableRules
                        .Select(r => new ConditionVariableRequirementDto { Id = r.Id }).ToList(),

                    OptionRequirements = c.OptionRules
                        .Select(r => new ConditionOptionRequirementDto
                        {
                            OptionItemId = r.OptionId,
                            SetKey = r.GroupKey,
                        }).ToList(),

                    ResourceRequirements = c.ResourceRules
                        .Select(r => new ConditionResourceRequirementDto
                        {
                            SetKey = r.GroupKey,
                            ResourceOptionItemId = r.SelectorItemId,
                        }).ToList(),

                    NumericRequirements = c.NumericRules
                        .Select(r => new ConditionNumericRequirementDto
                        {
                            SetKey = r.GroupKey,
                            MaxAllowedValue = r.MaxAllowedValue,
                            MinAllowedValue = r.MinAllowedValue,
                            NumericInputId = r.NumericQuestionId
                        }).ToList(),

                    // نحسب رابط المستأجر مرة واحدة لكل Assignment ثم نُسقِط
                    ConditionResourceAssignments = c.Assignments
                        .Select(a => new
                        {
                            a,
                            Link = a.Resource != null
                                ? a.Resource.TenantLinks
                                    .Where(x => x.TenantId == tenantid)
                                    .Select(x => new
                                    {
                                        x.AccountId,
                                        x.Name,
                                        x.ResourceTypeId,
                                        x.ResourceSortId,
                                        x.StatusId,
                                        x.Cost,
                                        x.Co2,
                                    })
                                    .FirstOrDefault()
                                : null
                        })
                        .Select(z => new ResourceAssignmentDto
                        {
                            Formulas = z.a.Expressions,
                            CapRole = z.a.CapacityRoles,
                            QuestionConditionId = z.a.ConditionId,
                            
                            Resource = z.a.Resource == null ? null : new ResourceDto
                            {
                                Id = z.a.ResourceId,
                                Name = z.a.Resource != null ? z.a.Resource.Name : string.Empty,
                                Active = z.a.IsActive,
                                CostRole = z.a.CapacityRoles,
                                CapRole = z.a.CapacityRoles,
                                CalcResCost = z.a.Resource.CalcResCost ?? new(),
                                MenuId = z.a.MenuId,
                                Data = new ResourceMetadata
                                {
                                    ChangeFactor1 = z.a.ChangeFactor1,
                                    ChangeFactor2 = z.a.ChangeFactor2,
                                    BaseCost = (decimal?)z.a.BaseCost,
                                    CapWaste = z.a.CapWaste,
                                    CO2 = z.Link == null ? null : z.Link.Co2,
                                    Cost = z.a.Resource.Data.Cost,
                                    Note = z.a.Resource.Data.Note,
                                    UpperNote = z.a.Resource.Data.UpperNote,
                                    Quantity = z.a.Resource.Data.Quantity,
                                    Unit = z.a.Resource.Data.Unit,
                                    QuantityParam = z.a.Resource.Data.QuantityParam,
                                },
                                FolderId = z.a.Resource.FolderId,
                                ResType = z.a.Resource.ResType,
                                SortOrder = z.a.Resource.SortOrder,

                                // --- الحقول المسطّحة بدل UserData ---
                                CostStorageValue = (double?)z.a.Resource.Data.Cost,
                                CostUserValue = z.Link == null ? null : z.Link.Cost,
                                NameUserValue = z.Link == null || z.Link.Name == null ? string.Empty : z.Link.Name,
                                AccountId = z.Link == null ? null : z.Link.AccountId,
                                ResourceTypeId = z.Link == null ? (int?)null : z.Link.ResourceTypeId,
                                ResourceSortId = z.Link == null ? (int?)null : z.Link.ResourceSortId,
                                StatusId = z.Link == null ? (int?)null : z.Link.StatusId,
                                // ------------------------------------

                                Properties = z.a.Resource.AttributeValues.Select(b => new ResourcePropertyBindDto
                                {
                                    Id = b.Id,
                                    NumberDefault = b.NumericValue,
                                    DataType = b.Attribute != null ? b.Attribute.DataType : default,
                                    DisplayName = b.Attribute != null ? b.Attribute.DisplayName : string.Empty,
                                    IsUserEditable = b.Attribute != null && b.Attribute.IsUserEditable,
                                    MaxNumericValue = b.Attribute != null ? b.Attribute.MaxNumericValue : null,
                                    TextDefault = b.TextValue,
                                }).ToList(),
                            },

                            NumericResourceFormulas = z.a.NumericAssignments
                                .Select(f => new NumericResourceAssignmentDto
                                {
                                    Formulas = f.Expressions,
                                    MaxInputValue = f.MaxInputValue,
                                    MinInputValue = f.MinInputValue,
                                    NumericId = f.NumericId
                                }).ToList(),

                            OptionResourceFormulas = z.a.OptionAssignments
                                .Select(f => new OptionResourceAssignmentDto
                                {
                                    Formulas = f.Expressions,
                                    ChoiceOptionId = f.OptionId
                                }).ToList()
                        })
                        .ToList()
                }).ToList()
            });
    }
}
