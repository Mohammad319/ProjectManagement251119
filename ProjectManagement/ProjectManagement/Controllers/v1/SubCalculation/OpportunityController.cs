using Application.Feature.Calculation.Opportunity.Commands;
using Application.Feature.Calculation.Opportunity.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Server.Controllers.v1.SubCalculation
{
    [ApiVersion("1.0")]
    public class OpportunityController : BaseApiController
    {
        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            return Ok(await MicroBus.Send(new GetOpportunitiesQuery(id)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost("{CalculationId}")]
        public async Task<IActionResult> Post(int CalculationId, PostOpportunityDTO dto)
        {
            return Ok(await MicroBus.Send(new CreateOpportunityCommand(dto, CalculationId)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, PostOpportunityDTO dto)
        {
            return Ok(await MicroBus.Send(new UpdateOpportunityCommand(dto, id)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            return Ok(await MicroBus.Send(new DeleteOpportunityCommand(id)));
        }
    }
}
