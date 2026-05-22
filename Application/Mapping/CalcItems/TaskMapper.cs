using Application.Extension;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Mapping.CalcItems
{
    public static class TaskMapper
    {
        public static List<TaskListDTO> MapToTaskListDTOs(this IEnumerable<TaskEntity> tasks)
        {
            return [.. tasks.Select(MapToTaskListDTO)];
        }

        public static TaskListDTO MapToTaskListDTO(this TaskEntity t)
        {
            var meta = t.GetMetadataSnapshot();

            return new TaskListDTO
            {
                TaskId = t.ParentTaskId,
                Id = t.Id,
                RowVersion = t.RowVersion,
                Name = t.Name,
                OpportunityId = t.OpportunityId,
                SortOrder = t.SortOrder,
                Quantity = t.Quantity,
                Unit = t.Unit ?? string.Empty,
                StatusId = t.StatusId,
                Status = t.Status?.Name ?? string.Empty,
                StatusColor = t.Status?.Color ?? string.Empty,
                Opportunity = t.Opportunity?.OpportunityType ?? string.Empty,
                Metadata = meta,
                Resources = t.Resources?.Select(ResourceExtention.MapToResourceListDTO).ToList() ?? [],
            };
        }

        public static TaskEntity MapToTaskEntity(TaskPostDTO dto, int calcId)
        {
            var task = TaskEntity.Create(calcId, dto, dto.SortOrder, dto.ParentTaskId);
            task.SetResources(dto.Resources?.Select(x => x.Parse(dto.Id)).ToList() ?? []);
            task.SetChildTasks(dto.Tasks?.Select(t => MapToTaskEntity(t, calcId)).ToList() ?? []);
            return task;
        }
    }
}
