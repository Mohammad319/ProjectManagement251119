using Application.Feature.Calculation.CalcShare.Commands;
using Application.Feature.Calculation.CalcShare.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;

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
        //public async Task<IActionResult> Post(PostShareCalcDTO dto)
        //{
        //    return Ok(await MicroBus.Send(new CreateCalcShareCommand(dto, GetUserId(), GetDepartmentId().Value)));
        //}
        [HttpPost]
        public async Task<IActionResult> Post(PostShareCalcDTO dto)
        {
            var departmentId = GetDepartmentId();
            if (!departmentId.HasValue)
                return BadRequest("Department is required.");

            var upsert = new ShareCalcUpsertDTO
            {
                Id = null,
                CalculationId = dto.CalculationId,
                DepartmentId = dto.DepartmentId,
                Tabs = ShareTabs.None, // أو حسب DTO عندك
                Tap1 = dto.Tap1,
                Tap2 = dto.Tap2,
                Tap3 = dto.Tap3,
                Tap4 = dto.Tap4,
                Tap5 = dto.Tap5,
                Tap6 = dto.Tap6
            };

            var id = await MicroBus.Send(new UpsertCalcShareCommand(upsert, GetUserId(), departmentId));
            return Ok(id);
        }

        [HttpPut]
        //public async Task<IActionResult> Update(UpdateShareCalcDTO dto)
        //{
        //    return Ok(await MicroBus.Send(new UpdateCalcShareCommand(dto, GetUserId(), GetDepartmentId().Value)));
        //}
        public async Task<IActionResult> Update(UpdateShareCalcDTO dto)
        {
            var departmentId = GetDepartmentId();
            if (!departmentId.HasValue)
                return BadRequest("Department is required.");

            var upsert = new ShareCalcUpsertDTO
            {
                Id = dto.Id,
                //CalculationId = dto.CalculationId,
                DepartmentId = departmentId.Value,
                Tabs = ShareTabs.None,
                Tap1 = dto.Tap1,
                Tap2 = dto.Tap2,
                Tap3 = dto.Tap3,
                Tap4 = dto.Tap4,
                Tap5 = dto.Tap5,
                Tap6 = dto.Tap6
            };

            var id = await MicroBus.Send(new UpsertCalcShareCommand(upsert, GetUserId(), departmentId));
            return Ok(id);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var groupId = GetDepartmentId();
            if (!groupId.HasValue)
                return BadRequest("Department is required.");

            return Ok(await MicroBus.Send(new DeleteCalcShareCommand(id)));
        }
    }
}
