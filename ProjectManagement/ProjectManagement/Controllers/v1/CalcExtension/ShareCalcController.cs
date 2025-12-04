using Application.Feature.Calculation.CalcShare.Commands;
using Application.Feature.Calculation.CalcShare.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Server.Controllers.v1.Calculation
{
    [ApiVersion("1.0"), Authorize(Roles = PMRolesConst.Tenant.Super_Manger)]
    public class ShareCalcController : BaseApiController
    {
        [HttpGet("{calcId}")]
        public async Task<IActionResult> GetAll(int calcId)
        {
            int? GroupId = GetDepartmentId();
            int UserId = GetUserId();

            return Ok(await MicroBus.Send(new GetShareQuery(calcId, GroupId, UserId)));
        }
        [HttpPost]
        public async Task<IActionResult> Post(PostShareCalcDTO dto)
        {
            return Ok(await MicroBus.Send(new CreateCalcShareCommand(dto, GetUserId(), GetDepartmentId().Value)));
        }
        [HttpPut]
        public async Task<IActionResult> Update(UpdateShareCalcDTO dto)
        {
            return Ok(await MicroBus.Send(new UpdateCalcShareCommand(dto, GetUserId(), GetDepartmentId().Value)));
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            int GroupId = GetDepartmentId().Value;
            int UserId = GetUserId();
            return Ok(await MicroBus.Send(new DeleteCalcShareCommand(id, GroupId, UserId)));
        }
    }
}
