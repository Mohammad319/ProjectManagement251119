using Application.Feature.Calculation.Resource.Commands;
using Application.Feature.Calculation.Resource.Queries;
using Application.Feature.Calculation.ResourceType.Queries;
using Application.Feature.Calculation.Task.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Server.Controllers.v1.SubCalculation
{
    [ApiVersion("1.0"), Authorize(Roles = PMRolesConst.Tenant.Users)]
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
        public async Task<IActionResult> ReOrder(int ResourceId, int NewOrder)
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

        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpGet("suggestions/{taskId:int}")]
        public async Task<IActionResult> GetSuggestions(
            int taskId,
            [FromQuery] int maxResults = 5,
            [FromQuery] bool includeResources = true)
        {
            var result = await MicroBus.Send(new GetTaskResourceSuggestionsQuery(taskId, maxResults, includeResources));
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpGet("suggestions/resources/{sourceTaskId:int}")]
        public async Task<IActionResult> GetSuggestionResources(
            int sourceTaskId,
            [FromQuery] TaskResourceSuggestionSource source)
        {
            var result = await MicroBus.Send(new GetTaskResourceSuggestionResourcesQuery(sourceTaskId, source));
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpPost("suggestions/feedback")]
        public async Task<IActionResult> RecordSuggestionFeedback([FromBody] TaskResourceSuggestionFeedbackDTO feedback)
        {
            var result = await MicroBus.Send(new RecordTaskResourceSuggestionFeedbackCommand(feedback));
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpPost("suggestions/bulk")]
        public async Task<IActionResult> GetBulkSuggestions(
            [FromBody] List<int> taskIds,
            [FromQuery] int maxResultsPerTask = 10)
        {
            var result = await MicroBus.Send(new GetBulkTaskResourceSuggestionsQuery(taskIds, maxResultsPerTask));
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
