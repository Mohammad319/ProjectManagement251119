using Microsoft.EntityFrameworkCore;
using ProjectImportHub.Entities;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
namespace ProjectManagement.Shared.Mappers;

public static class ProjectTaskProjection
{
    public static IQueryable<ProjectTaskDto> TasksBaseToDto(
    this IQueryable<ProjectTaskEntity> query, int tenantid)
    {
        return query.AsNoTracking().AsSplitQuery()
            .Select(t => new ProjectTaskDto
            {
                Id = t.Id,
                DisplayName = t.DisplayName,
                SortOrder = t.SortOrder,
                IsVisible = t.IsVisible,
                CapacityResourceId = t.CapacityResourceId,
                UnitGroupId = t.UnitGroupId,
                Code = t.Code,
                NewUnitCode = t.NewUnitCode,
                Note = t.Note,
                UnitCode = t.UnitCode,
                WorkloadThresholds = t.WorkloadThresholds,
                Quantity = t.Quantity,
                Uncontrollable = t.Uncontrollable,
                UnitGroup = t.UnitGroup == null ? null : new UnitGroupDto
                {
                    Id = t.UnitGroup.Id,
                    DisplayName = t.UnitGroup.DisplayName,
                    Keys = t.UnitGroup.Keys,
                },

                BaseResources = t.TaskResourceAssignments.Select(a => new
                    {
                        a,
                        Link = a.Resource.ResourcesTenant
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
                            .FirstOrDefault() // APPLY/LEFT JOIN واحد فقط
                    })
                    .Select(z => new ResourceDto
                    {
                        Id = z.a.ResourceId,
                        Name = z.a.Resource.Name,
                        CalcResCost = z.a.Resource.CalcResCost ?? new (),
                        Active = z.a.IsActive,
                        FolderId = z.a.Resource.FolderId,
                        ResType = z.a.Resource.ResType,
                        SortOrder = z.a.Resource.SortOrder,
                        CostRole = z.a.CapRole,
                        CostStorageValue = z.a.Resource.Data.Cost,
                        CostUserValue = z.Link == null ? null : z.Link.Cost,
                        NameUserValue = z.Link == null ? null : z.Link.Name,

                        AccountId = z.Link == null ? null : z.Link.AccountId,
                        ResourceTypeId = z.Link == null ? (int?)null : z.Link.ResourceTypeId,
                        ResourceSortId = z.Link == null ? (int?)null : z.Link.ResourceSortId,
                        StatusId = z.Link == null ? (int?)null : z.Link.StatusId,
                        MenuId = z.a.MenuId,
                        Properties = z.a.Resource.PropertiesBind
                            .Select(b => new ResourcePropertyBindDto
                            {
                                Id = b.PropertyId,
                                NumberDefault = b.NumberDefault,
                                DataType = b.Property.DataType,
                                DisplayName = b.Property.DisplayName,
                                IsUserEditable = b.Property.IsUserEditable,
                                MaxNumericValue = b.Property.MaxNumericValue,
                                TextDefault = b.TextDefault,
                            }).ToList(),

                        Data = new ResourceData
                        {
                            ChangeFactor1 = z.a.ChangeFactor1,
                            ChangeFactor2 = z.a.ChangeFactor2,
                            CapWaste = z.a.CapWaste,
                            BaseCost = z.a.BaseCost,
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
    this IQueryable<ProjectTaskEntity> query, int tenantid, int depid)
    {
        return query.AsNoTracking().AsSplitQuery()
            .Select(t => new ProjectTaskDto
            {
                Id = t.Id,
                DisplayName = t.DisplayName,
                SortOrder = t.SortOrder,
                IsVisible = t.IsVisible,
                CapacityResourceId = t.CapacityResourceId,
                UnitGroupId = t.UnitGroupId,
                Code = t.Code,
                NewUnitCode = t.NewUnitCode,
                Note = t.Note,
                UnitCode = t.UnitCode,
                WorkloadThresholds = t.WorkloadThresholds,
                Quantity = t.Quantity,
                Uncontrollable = t.Uncontrollable,
                UpperNote = t.UpperNote,
               
                UnitGroup = t.UnitGroup == null ? null : new UnitGroupDto
                {
                    Id = t.UnitGroup.Id,
                    DisplayName = t.UnitGroup.DisplayName,
                    Keys = t.UnitGroup.Keys,
                },
                OptionGroups = t.OptionGroups.Select(g => new OptionGroupDto
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
                        OptionGroupId = o.OptionGroupId,
                    }).ToList()
                }).ToList(),
                ResourceOptionGroups = t.ResourceOptionGroups.Select(g => new ResourceOptionGroupDto
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
                NumericInputs = t.NumericInputs.Select(n => new NumericInputDto
                {
                    Id = n.Id,
                    DisplayName = n.DisplayName,
                    SortOrder = n.SortOrder,
                    //TaskId = n.TaskId,
                    MaxInputValue = n.MaxInputValue,
                    MinInputValue = n.MinInputValue,
                    SectionKey = n.SectionKey,
                }).ToList(),
                Conditions = t.Conditions.Select(c => new TaskConditionDto
                {
                    Id = c.Id,
                    NumericToResourceLogic = c.NumericToResourceLogic,
                    OptionToNumericLogic = c.OptionToNumericLogic,
                    OptionToResourceLogic = c.OptionToResourceLogic,

                    VariableRequirements = c.VariableRequirements
                        .Select(r => new ConditionVariableRequirementDto { Id = r.Id }).ToList(),

                    OptionRequirements = c.OptionRequirements
                        .Select(r => new ConditionOptionRequirementDto
                        {
                            OptionItemId = r.OptionItemId,
                            SetKey = r.SetKey,
                        }).ToList(),

                    ResourceRequirements = c.ResourceRequirements
                        .Select(r => new ConditionResourceRequirementDto
                        {
                            SetKey = r.SetKey,
                            ResourceOptionItemId = r.ResourceOptionItemId,
                        }).ToList(),

                    NumericRequirements = c.NumericRequirements
                        .Select(r => new ConditionNumericRequirementDto
                        {
                            SetKey = r.SetKey,
                            MaxAllowedValue = r.MaxAllowedValue,
                            MinAllowedValue = r.MinAllowedValue,
                            NumericInputId = r.NumericInputId
                        }).ToList(),

                    // نحسب رابط المستأجر مرة واحدة لكل ResourceAssignment ثم نُسقِط
                    ConditionResourceAssignments = c.ConditionResourceAssignments
                        .Select(a => new
                        {
                            a,
                            Link = a.Resource.ResourcesTenant
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
                        })
                        .Select(z => new ResourceAssignmentDto
                        {
                            Formulas = z.a.Formulas,
                            CapRole = z.a.CapRole,
                            QuestionConditionId = z.a.QuestionConditionId,
                            
                            Resource = z.a.Resource == null ? null : new ResourceDto
                            {
                                Id = z.a.ResourceId,
                                Name = z.a.Resource.Name,
                                Active = z.a.IsActive,
                                CostRole = z.a.CapRole,
                                CapRole = z.a.CapRole,
                                CalcResCost = z.a.Resource.CalcResCost ?? new(),
                                MenuId = z.a.MenuId,
                                Data = new ResourceData
                                {
                                    ChangeFactor1 = z.a.ChangeFactor1,
                                    ChangeFactor2 = z.a.ChangeFactor2,
                                    BaseCost = z.a.BaseCost,
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
                                CostStorageValue = z.a.Resource.Data.Cost,
                                CostUserValue = z.Link == null ? null : z.Link.Cost,
                                NameUserValue = z.Link == null ? null : z.Link.Name,
                                AccountId = z.Link == null ? null : z.Link.AccountId,
                                ResourceTypeId = z.Link == null ? (int?)null : z.Link.ResourceTypeId,
                                ResourceSortId = z.Link == null ? (int?)null : z.Link.ResourceSortId,
                                StatusId = z.Link == null ? (int?)null : z.Link.StatusId,
                                // ------------------------------------

                                Properties = z.a.Resource.PropertiesBind.Select(b => new ResourcePropertyBindDto
                                {
                                    Id = b.Id,
                                    NumberDefault = b.NumberDefault,
                                    DataType = b.Property.DataType,
                                    DisplayName = b.Property.DisplayName,
                                    IsUserEditable = b.Property.IsUserEditable,
                                    MaxNumericValue = b.Property.MaxNumericValue,
                                    TextDefault = b.TextDefault,
                                }).ToList(),
                            },

                            NumericResourceFormulas = z.a.NumericResourceFormulas
                                .Select(f => new NumericResourceAssignmentDto
                                {
                                    Formulas = f.Formulas,
                                    MaxInputValue = f.MaxInputValue,
                                    MinInputValue = f.MinInputValue,
                                    NumericId = f.NumericId
                                }).ToList(),

                            OptionResourceFormulas = z.a.OptionResourceFormulas
                                .Select(f => new OptionResourceAssignmentDto
                                {
                                    Formulas = f.Formulas,
                                    ChoiceOptionId = f.ChoiceOptionId
                                }).ToList()
                        })
                        .ToList()
                }).ToList()
            });
    }
}
