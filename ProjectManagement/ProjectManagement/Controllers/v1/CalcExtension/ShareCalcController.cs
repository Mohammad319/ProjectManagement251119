using Application.Feature.Calculation.CalcShare.Commands;
using Application.Feature.Calculation.CalcShare.Queries;
using Application.Mapping.Calculation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Server.Controllers.v1.Calculation
{
    [ApiVersion("1.0"), Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
    public class ShareCalcController : BaseApiController
    {
        [HttpGet("{calcId}")]
        public async Task<IActionResult> GetAll(int calcId)
        {
            int? groupId = GetDepartmentId();
            int userId = GetUserId();

            return Ok(await MicroBus.Send(new GetShareQuery(calcId, groupId, userId)));
        }

        [HttpPost]
        public async Task<IActionResult> Post(PostShareCalcDTO dto)
        {
            var id = await MicroBus.Send(new UpsertCalcShareCommand(dto.ToUpsert(), GetUserId(), GetDepartmentId()));
            return Ok(id);
        }

        [HttpPut]
        public async Task<IActionResult> Update(UpdateShareCalcDTO dto)
        {
            var departmentId = GetDepartmentId();
            if (!departmentId.HasValue)
                return BadRequest("Department not found.");

            var id = await MicroBus.Send(new UpsertCalcShareCommand(dto.ToUpsert(departmentId.Value), GetUserId(), GetDepartmentId()));
            return Ok(id);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var groupId = GetDepartmentId();
            if (!groupId.HasValue)
                return BadRequest("Department not found.");

            return Ok(await MicroBus.Send(new DeleteCalcShareCommand(id)));
        }
    }
}
