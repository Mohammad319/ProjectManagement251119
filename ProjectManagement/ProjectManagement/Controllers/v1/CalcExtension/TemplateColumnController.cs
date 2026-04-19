using Application.Feature.Calculation.TemplateTable.Commands;
using Application.Feature.Calculation.TemplateTable.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace ProjectManagement.Server.Controllers.v1.Calculation
{
    [ApiVersion("1.0")]
    public class TemplateColumnController : BaseApiController
    {
        [Authorize(Roles = PMRolesConst.Tenant.Users), HttpGet(URLConst.GetAll)]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await MicroBus.Send(new GetTemplateColumnsByUserQuery(GetDepartmentId())));
        }

        [Authorize(Roles = PMRolesConst.Tenant.Users), HttpGet(URLConst.TemplateColumn.GetById + "/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await MicroBus.Send(new GetTemplateColumnByIdQuery(id, GetDepartmentId())));
        }

        [Authorize(Roles = PMRolesConst.Tenant.Users), HttpGet(URLConst.TemplateColumn.GetByDepartment + "/{department}")]
        public async Task<IActionResult> GetByDepartment(int department)
        {
            return Ok(await MicroBus.Send(new GetTemplateColumnsByUserQuery(department)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger), HttpGet(URLConst.TemplateColumn.Set + "/{id}/{templateColumnId?}")]
        public async Task<IActionResult> SetDefault(int id, int? templateColumnId)
        {
            return Ok(await MicroBus.Send(new SetDefaultTemplateColumnCommand(id, templateColumnId, GetDepartmentId())));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminSuperManger)]
        [HttpPost]
        public async Task<IActionResult> Post(TemplateColumnPostDTO dto)
        {
            var departmentId = GetDepartmentId();
            if (!departmentId.HasValue)
                return BadRequest();

            return Ok(await MicroBus.Send(new CreateTemplateColumnCommand(dto, departmentId)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.Admin)]
        [HttpPost(URLConst.TemplateColumn.PostAdmin + "/{departmentId?}")]
        public async Task<IActionResult> PostAdmin(TemplateColumnPostDTO dto, int? departmentId)
        {
            return Ok(await MicroBus.Send(new CreateTemplateColumnCommand(dto, departmentId)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut(URLConst.TemplateColumn.Update + "/{id}")]
        public async Task<IActionResult> Update(int id, TemplateColumnPostDTO dto)
        {
            return Ok(await MicroBus.Send(new UpdateTemplateColumnCommand(dto, id, GetDepartmentId())));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminSuperManger)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            return Ok(await MicroBus.Send(new DeleteTemplateColumnCommand(id, GetDepartmentId())));
        }
    }
}
