using Application.Feature.Calculation.Resource.Commands;
using Application.Feature.Calculation.Resource.Queries;
using Application.Feature.Calculation.ResourceType.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Server.Controllers.v1.SubCalculation
{
    [ApiVersion("1.0"), Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
    public class ResourcesController() : BaseApiController
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await MicroBus.Send(new GetVisualResourcesQuery()));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpGet(URLConst.ReOrder + "/{ResourceId}/{NewOrder}")]
        public async Task<IActionResult> ReOrder(int ResourceId, double NewOrder) =>
            Ok(await MicroBus.Send(new NewOrderResourceCommand(ResourceId, NewOrder)));

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost("{id}")]
        public async Task<IActionResult> Post(int id, [FromBody] List<ResourcePostDTO> Items)
        {
            return Ok(await MicroBus.Send(new CreateResourceCommand(Items, id)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost(URLConst.Filter)]
        public async Task<IActionResult> Post(GetResourcesByFilterQuery command)
        {
            return Ok(await MicroBus.Send(command));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ResourcePostDTO dto)
        {
            return Ok(await MicroBus.Send(new UpdateResourceCommand(id, dto)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpDelete("{CalcID}")]
        public async Task<IActionResult> Delete(int CalcID, [FromBody] IEnumerable<int> items) =>
            Ok(await MicroBus.Send(new DeleteResourceCommand(items, CalcID)));
    }
}
