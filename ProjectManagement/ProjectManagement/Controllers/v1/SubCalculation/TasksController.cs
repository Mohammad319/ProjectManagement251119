using Application.Feature.Calculation.Group.Queries;
using Application.Feature.Calculation.Task.Commands;
using Application.Feature.Calculation.TaskStatus.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Server.Controllers.v1.SubCalculation
{
    [ApiVersion("1.0"), Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
    public class TasksController() : BaseApiController
    {
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpGet(URLConst.ReOrder + "/{TaskId}/{NewOrder}")]
        public async Task<IActionResult> ReOrder(int TaskId, int NewOrder)
        {
            var result = await MicroBus.Send(new NewOrderTaskCommand(TaskId, NewOrder));
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpGet("status")]
        public async Task<IActionResult> Get(int? id)
        {
            var result = await MicroBus.Send(new GetVisualTaskStatusQuery(id));
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost("{id}")]
        public async Task<IActionResult> Create(int id, [FromBody] List<TaskPostDTO> Tasks)
        {
            var result = await MicroBus.Send(new CreateTaskCommand(Tasks, id));
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost(URLConst.Filter)]
        public async Task<IActionResult> Filter([FromBody] FilterCalculationItemsDto obj)
        {
            var result = await MicroBus.Send(new GetTasksByFilterQuery(obj));
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TaskPostDTO dto)
        {
            // Allow If-Match / ETag based concurrency (optional) without breaking body-based RowVersion
            TrySetRowVersionFromIfMatch(dto);

            var result = await MicroBus.Send(new UpdateTaskCommand(id, dto));
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpDelete("{calcID}")]
        public async Task<IActionResult> Delete(int calcID, [FromBody] IEnumerable<int> items)
        {
            var result = await MicroBus.Send(new DeleteTaskCommand(items, calcID));
            TrySetETag(result);
            return Ok(result);
        }
    }
}
