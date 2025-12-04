using Application.Feature.Application.Commands;
using Application.Feature.Application.Queries;
using Domain.Entities.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Base.Application;
using ProjectManagement.Shared.Constant;
using static ProjectManagement.Shared.Constant.PMRolesConst;

namespace ProjectManagement.Server.Controllers.v1.TemplateCalc
{
    [ApiVersion("1.0")]
    public class ApplicationController : BaseApiController
    {
        [Authorize(Roles = Tenant.Users), HttpGet]
        public async Task<IActionResult> GetApplications(bool wnv = false)
        {
            return Ok(await MicroBus.Send(new GetApplicationQuery(wnv)));
        }
        [Authorize(Roles = Tenant.Users), HttpGet(URLConst.Application.AppCalculationValues + "/{calcId}")]
        public async Task<IActionResult> GetCalcAppValues(int calcId)
        {
            return Ok(await MicroBus.Send(new GetCalcAppQuery(calcId)));
        }

        [Authorize(Roles = Tenant.AdminManger)]
        [HttpPost(URLConst.Application.AppCalculationValues)]
        public async Task<IActionResult> Post(ApplicationValuesBase Dto)
        {
            Dto.UserId = GetUserId();
            return Ok(await MicroBus.Send(new CreateCalcAppCommand(Dto,0,0)));
        }

        [Authorize(Roles = Tenant.AdminManger)]
        [HttpPut(URLConst.Application.AppCalculationValues)]
        public async Task<IActionResult> Put(ApplicationValuesEntity dto)
        {
            return Ok(await MicroBus.Send(new UpdateCalcAppCommand(dto)));
        }

        [Authorize(Roles = Tenant.AdminManger)]
        [HttpDelete(URLConst.Application.AppCalculationValues + "/{id}")]
        public async Task<IActionResult> DeleteCalcApp(int id)
        {
            return Ok(await MicroBus.Send(new DeleteCalcAppCommand (id)));
        }
    }
}
