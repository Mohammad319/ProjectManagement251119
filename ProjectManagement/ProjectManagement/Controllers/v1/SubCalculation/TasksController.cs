using Application.Feature.Calculation.Group.Queries;
using Application.Feature.Calculation.Task.Commands;
using Application.Feature.Project.TaskStatus.Queries;
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
        public async Task<IActionResult> ReOrder(int TaskId, double NewOrder) =>
            Ok(await MicroBus.Send(new NewOrderTaskCommand(TaskId, NewOrder)));
        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpGet("status")]
        public async Task<IActionResult> Get(int? id)
        {
            return Ok(await MicroBus.Send(new GetVisualTaskStatusQuery(id)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost("{id}")]
        public async Task<IActionResult> Create(int id, [FromBody] List<TaskPostDTO> Tasks)
        {
            return Ok(await MicroBus.Send(new CreateTaskCommand(Tasks,id)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost(URLConst.Filter)]
        public async Task<IActionResult> Post(FilterCalculationItemsDto obj)
        {
            return Ok(await MicroBus.Send(new GetTasksByFilterQuery(obj)));
        }


        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TaskPostDTO dto)
        {
            return Ok(await MicroBus.Send(new UpdateTaskCommand(id,dto)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpDelete("{calcID}")]
        public async Task<IActionResult> Delete(int calcID, [FromBody] IEnumerable<int> items) =>
            Ok(await MicroBus.Send(new DeleteTaskCommand (items,calcID)));
    }
}
