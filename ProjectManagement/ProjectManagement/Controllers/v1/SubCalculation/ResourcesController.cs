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
            var result = await MicroBus.Send(new GetVisualResourcesQuery());
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpGet(URLConst.ReOrder + "/{ResourceId}/{NewOrder}")]
        public async Task<IActionResult> ReOrder(int ResourceId, double NewOrder)
        {
            var result = await MicroBus.Send(new NewOrderResourceCommand(ResourceId, NewOrder));
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost("{id}")]
        public async Task<IActionResult> Post(int id, [FromBody] List<ResourcePostDTO> Items)
        {
            var result = await MicroBus.Send(new CreateResourceCommand(Items, id));
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost(URLConst.Filter)]
        public async Task<IActionResult> Post([FromBody] GetResourcesByFilterQuery command)
        {
            var result = await MicroBus.Send(command);
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ResourcePostDTO dto)
        {
            // Allow If-Match / ETag based concurrency (optional) without breaking body-based RowVersion
            TrySetRowVersionFromIfMatch(dto);

            var result = await MicroBus.Send(new UpdateResourceCommand(id, dto));
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpDelete("{CalcID}")]
        public async Task<IActionResult> Delete(int CalcID, [FromBody] IEnumerable<int> items)
        {
            var result = await MicroBus.Send(new DeleteResourceCommand(items, CalcID));
            TrySetETag(result);
            return Ok(result);
        }
    }
}
