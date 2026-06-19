using Application.Feature.Project.ProjectShare.Commands;
using Application.Feature.Project.ProjectShare.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Server.Controllers.v1.Project
{
    [ApiVersion("1.0")]
    public class ProjectSharesController : BaseApiController
    {
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpGet("{projectId}")]
        public async Task<IActionResult> GetByProject(Guid projectId)
            => Ok(await MicroBus.Send(new GetProjectSharesQuery(projectId, GetDepartmentId(), GetUserId())));

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost("{projectId}")]
        public async Task<IActionResult> Upsert(Guid projectId, ProjectShareUpsertDTO dto)
            => Ok(await MicroBus.Send(new UpsertProjectShareCommand(projectId, dto, GetUserId(), GetDepartmentId())));

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
            => Ok(await MicroBus.Send(new DeleteProjectShareCommand(id, GetUserId(), GetDepartmentId())));
    }
}
