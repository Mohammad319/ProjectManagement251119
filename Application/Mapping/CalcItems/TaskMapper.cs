using Application.Extention;
using Domain.Entities.Calculation;
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
            return new TaskListDTO
            {
                TaskId = t.ParentTaskId,
                Id = t.Id,
                Name = t.Name,
                OpportunityId = t.OpportunityId,
                Order = t.SortOrder,
                StatusId = t.StatusId,
                Status = t.Status?.Name ?? string.Empty,
                StatusColor = t.Status?.Color ?? string.Empty,
                Opportunity = t.Opportunity?.OpportunityType ?? string.Empty,
                Data = t.Metadata,
                Resources = t.Resources?.Select(ResourceExtention.MapToResourceListDTO).ToList() ?? [],

            };
        }
        public static TaskEntity MapToTaskEntity(TaskPostDTO dto, int calcId)
        {
            var task = TaskEntity.Create(calcId,dto, dto.Order, dto.ParentTaskId);

            task.OpportunityId = dto.OpportunityId;
            task.StatusId = dto.StatusId;
            task.Metadata = dto.Metadata;

            task.Resources = dto.Resources?.Select(x => x.Parse(dto.Id)).ToList() ?? [];
            task.Tasks = dto.Tasks?.Select(t => MapToTaskEntity(t, calcId)).ToList() ?? [];

            return task;
        }

        //public static TaskEntity MapToTaskEntity(TaskPostDTO dto, int calcId) => new()
        //{
        //    CalculationId = calcId,
        //    Name = dto.Name,
        //    OpportunityId = dto.OpportunityId,
        //    StatusId = dto.StatusId,
        //    ParentTaskId = dto.ParentTaskId,
        //    SortOrder = dto.Order,
        //    Metadata = dto.Metadata,
        //    Resources = dto.Resources?.Select(x => x.Parse(dto.Id)).ToList(),
        //    Tasks = dto.Tasks?.Select(t => MapToTaskEntity(t, calcId)).ToList() ?? [],
        //};
    }
}
