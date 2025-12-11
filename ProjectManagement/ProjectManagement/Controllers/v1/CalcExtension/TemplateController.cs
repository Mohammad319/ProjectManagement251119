using Application.Feature.Calculation.TemplateTable.Commands;
using Application.Feature.Calculation.TemplateTable.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace ProjectManagement.Server.Controllers.v1.Calculation
{
    [ApiVersion("1.0")]
    public class TemplateController : BaseApiController
    {

        [Authorize(Roles = PMRolesConst.Tenant.Users), HttpGet(URLConst.GetAll)]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await MicroBus.Send(new GetTemplatesByUserQuery( GetDepartmentId())));
        }
        [Authorize(Roles = PMRolesConst.Tenant.Users), HttpGet(URLConst.Template.GetById + "/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await MicroBus.Send(new GetTemplateByIdQuery(id, GetDepartmentId())));
        }
        [Authorize(Roles = PMRolesConst.Tenant.Users), HttpGet(URLConst.Template.GetByDepartment + "/{department}")]
        public async Task<IActionResult> GetByDepartment(int department)
        {
            return Ok(await MicroBus.Send(new GetTemplatesByUserQuery(department)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger), HttpGet(URLConst.Template.Set + "/{id}/{tempId?}")]
        public async Task<IActionResult> SetDefault(int id, int? tempId)
        {
            return Ok(await MicroBus.Send(new SetDefaultTemplateCommand(id, tempId, GetDepartmentId())));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminSuperManger)]
        [HttpPost]
        public async Task<IActionResult> Post(TemplateListPostDTO dto)
        {
            var DepartmentId = GetDepartmentId();
            if (!DepartmentId.HasValue)
                return BadRequest();
            return Ok(await MicroBus.Send(new CreateTemplateCommand(dto, DepartmentId)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.Admin)]
        [HttpPost(URLConst.Template.PostAdmin + "/{departmentId?}")]
        public async Task<IActionResult> PostAdmin(TemplateListPostDTO dto, int? departmentId)
        {
            return Ok(await MicroBus.Send(new CreateTemplateCommand(dto, departmentId)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut(URLConst.Template.Update + "/{id}")]
        public async Task<IActionResult> Update(int id, TemplateListPostDTO dto)
        {
            return Ok(await MicroBus.Send(new UpdateTemplateCommand(dto,id, GetDepartmentId())));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminSuperManger)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            return Ok(await MicroBus.Send(new DeleteTemplateCommand(id, GetDepartmentId() )));
        }
    }
}
