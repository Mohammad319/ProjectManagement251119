using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Entities.Tasks;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
namespace TaskResourceBlueprints.Mappers.Shared.Mappers;

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
                Code = t.Code,
                SearchText = t.NormalizedTextSv,
                NewUnitCode = t.NewUnitCode,
                Note = t.FieldNotes,
                UnitCode = t.UnitCode,
                WorkloadThresholds = t.WorkloadThresholds,
                Quantity = t.Quantity,
                PriceProduction = t.PriceProduction ?? 0m,
                ChangeFactor1 = t.ChangeFactor1,
                ChangeFactor2 = t.ChangeFactor2,
                Uncontrollable = t.Uncontrollable,
                StateLinks = t.StateLinks.Select(sl => new TaskStateLinkDto
                {
                    GroupId = sl.State!.TaskStateGroupId,
                    GroupName = sl.State.Group!.Name,
                    StateId = sl.TaskStateId,
                    StateName = sl.State.Name,
                }).ToList(),
                BaseResources = new List<ResourceDto>(),
            });
    }

    public static IQueryable<ProjectTaskDto> ProjectToDto(
    this IQueryable<TaskDefinition> query, int tenantid, int _depid)
    {
        return query.AsNoTracking().AsSplitQuery()
            .Select(t => new ProjectTaskDto
            {
                Id = t.Id,
                DisplayName = t.Name,
                SortOrder = t.SortOrder,
                IsVisible = t.IsVisible,
                CapacityResourceId = t.CapacityResourceId,
                Code = t.Code,
                SearchText = t.NormalizedTextSv,
                NewUnitCode = t.NewUnitCode,
                Note = t.FieldNotes,
                UnitCode = t.UnitCode,
                WorkloadThresholds = t.WorkloadThresholds,
                Quantity = t.Quantity,
                PriceProduction = t.PriceProduction ?? 0m,
                ChangeFactor1 = t.ChangeFactor1,
                ChangeFactor2 = t.ChangeFactor2,
                Uncontrollable = t.Uncontrollable,
                UpperNote = t.RowNotes,
                StateLinks = t.StateLinks.Select(sl => new TaskStateLinkDto
                {
                    GroupId = sl.State!.TaskStateGroupId,
                    GroupName = sl.State.Group!.Name,
                    StateId = sl.TaskStateId,
                    StateName = sl.State.Name,
                }).ToList(),
                BaseResources = new List<ResourceDto>(),
            });
    }
}
