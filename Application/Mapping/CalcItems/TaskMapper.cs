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
                TaskId = t.TaskId,
                Id = t.Id,
                Name = t.Name,
                OpportunityId = t.OpportunityId,
                Order = t.Order,
                StatusId = t.StatusId,
                Status = t.Status?.Name ?? string.Empty,
                StatusColor = t.Status?.Color ?? string.Empty,
                Opportunity = t.Opportunity?.Type ?? string.Empty,
                Data = t.Data,
                Resources = t.Resources?.Select(ResourceExtention.MapToResourceListDTO).ToList() ?? [],

            };
        }

        public static TaskEntity MapToTaskEntity(TaskPostDTO dto, int calcId) => new()
        {
            CalculationId = calcId,
            Name = dto.Name,
            OpportunityId = dto.OpportunityId,
            StatusId = dto.StatusId,
            TaskId = dto.TaskId,
            Order = dto.Order,
            Data = dto.Data,
            Resources = dto.Resources?.Select(x => x.Parse(dto.Id)).ToList(),
            Tasks = dto.Tasks?.Select(t => MapToTaskEntity(t, calcId)).ToList() ?? [],
        };
    }
}
