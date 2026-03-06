using Application.Feature.Calculation.Calculation.Commands;
using Application.Feature.Calculation.Calculation.Queries;
using Application.Feature.Project.Type.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using static ProjectManagement.Shared.Constant.PMRolesConst;

namespace ProjectManagement.Server.Controllers.v1.Project
{
    [ApiVersion("1.0")]
    public class CalculationsController : BaseApiController
    {
        [Authorize(Roles = Tenant.Users)]
        [HttpGet("config/{m}/{con}/{com}/{t}/{st}")]
        public async Task<IActionResult> Config(int m, int con, int com, int t, int st)
        {
            return Ok(await MicroBus.Send(new GetProjectCalcConfigQuery() { Methods = m, Contracts = con, Compensations = com, Types = t, Statuses = st, TypeObj = 0 }));
        }
        [Authorize(Roles = Tenant.AdminManger)]
        [HttpGet(URLConst.ReOrder + "/{Id}/{NewOrder}")]
        public async Task<IActionResult> ReOrder(int Id, int NewOrder)
        {
            return Ok(await MicroBus.Send(new NewOrderCalculationCommand(Id, NewOrder)));
        }

        [Authorize(Roles = Tenant.AdminManger)]
        [HttpGet(URLConst.Calculation.Copy + "/{projectId}/{calcId}")]
        public async Task<IActionResult> Copy(Guid projectId, int calcId)
        {
            return Ok(await MicroBus.Send(new CopyCalculationCommand(calcId, projectId, GetDepartmentId(), GetUserId())));
        }

        [Authorize(Roles = Tenant.Users)]
        [HttpGet("{projectId}")]
        public async Task<IActionResult> Get(Guid projectId)
        {
            return Ok(await MicroBus.Send(new GetAllCalculationsQuery(projectId, GetUserId(), GetDepartmentId())));
        }
        [Authorize(Roles = Tenant.Users)]
        [HttpGet(URLConst.Calculation.HourlyPriceList + "/{id}")]
        public async Task<IActionResult> Get(int id)
        {
            return Ok(await MicroBus.Send(new HourlyPriceListQuery(id, GetDepartmentId())));
        }
        [Authorize(Roles = Tenant.Users)]
        [HttpGet(URLConst.Calculation.Share + "/{projectId}")]
        public async Task<IActionResult> GetShareCalculations(Guid projectId)
        {
            return Ok(await MicroBus.Send(new GetAllCalculationsByDepartmentQuery(projectId, GetUserId(), GetDepartmentId())));
        }
        [Authorize(Roles = Tenant.Users)]
        [HttpGet(URLConst.Details + "/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            return Ok(await MicroBus.Send(new GetCalculationDetailsQuery(id)));
        }
        [Authorize(Roles = Tenant.Users)]
        [HttpGet(URLConst.Calculation.Page + "/{id}")]
        public async Task<IActionResult> GetPage1(int id)
        {
            return Ok(await MicroBus.Send(new GetCalculationPageQuery(id, GetUserId(), GetDepartmentId())));
        }

        [Authorize(Roles = Tenant.Users)]
        [HttpGet(URLConst.Calculation.SharedPage + "/{id}")]
        public async Task<IActionResult> GetPage2(int id)
        {
            var departmentId = GetDepartmentId();
            if (departmentId is null) return BadRequest();
            return Ok(await MicroBus.Send(new GetShareCalculationPageQuery(id, GetUserId(), departmentId.Value)));
        }
        [Authorize(Roles = Tenant.Users)]
        [HttpGet(URLConst.Calculation.GetToPost + "/{id}")]
        public async Task<IActionResult> GetPost(int id)
        {
            return Ok(await MicroBus.Send(new GetCalculationPostQuery(id)));
        }
        [Authorize(Roles = Tenant.AdminManger)]
        [HttpPost(URLConst.Calculation.Create + "/{ProjectId}")]
        public async Task<IActionResult> Post(Guid ProjectId, CalculationPostDTO Dto)
        {
            return Ok(await MicroBus.Send(new CreateCalculationCommand(Dto, ProjectId, GetUserId(), GetDepartmentId())));
        }
        [Authorize(Roles = Tenant.AdminManger)]
        [HttpPost]
        [Route(URLConst.Calculation.HourlyPriceList + "/{id}")]
        public async Task<IActionResult> Post(int id, [FromBody] List<HourlyPriceListGroupDTO> hourlyPriceList)
        {
            return Ok(await MicroBus.Send(new HourlyPriceListCommand(id, hourlyPriceList, GetUserId(), GetDepartmentId())));
        }
        [Authorize(Roles = Tenant.AdminManger)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CalculationPostDTO dto)
        {
            return Ok(await MicroBus.Send(new UpdateCalculationCommand(dto, id, GetUserId(), GetDepartmentId())));
        }

        [Authorize(Roles = Tenant.AdminManger)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            return Ok(await MicroBus.Send(new DeleteCalculationCommand(id, GetUserId(), GetDepartmentId())));
        }
        [Authorize(Roles = Tenant.AdminManger)]
        [HttpPut("factors/{id}")]
        public async Task<IActionResult> UpdateFactors(int id, List<OHFactors> model)
        {
            return Ok(await MicroBus.Send(new UpdateFactorsCommand(model, id, GetDepartmentId())));
        }
        //[Authorize(Roles = Tenant.AdminManger)]
        [HttpPut("QuantityList/{id}")]
        public async Task<IActionResult> UpdateQuantityList(int id, List<QuanityListDTO> model)
        {
            return Ok(await MicroBus.Send(new UpdateQuantityListCommand(model, id, GetDepartmentId())));
        }

    }
}
