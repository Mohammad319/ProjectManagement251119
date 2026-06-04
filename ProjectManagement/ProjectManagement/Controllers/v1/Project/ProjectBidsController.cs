using Application.Feature.Project.ProjectBid;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Server.Controllers.v1.Project
{
    [ApiVersion("1.0")]
    public class ProjectBidsController : BaseApiController
    {
        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpGet("{projectId}")]
        public async Task<IActionResult> GetAll(Guid projectId)
            => Ok(await MicroBus.Send(new GetProjectBidsQuery(projectId, GetDepartmentId())));

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost("{projectId}")]
        public async Task<IActionResult> Create(Guid projectId, ProjectBidPostDTO dto)
            => Ok(await MicroBus.Send(new CreateProjectBidCommand(projectId, dto, GetDepartmentId())));

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut("{id}/{projectId}")]
        public async Task<IActionResult> Update(int id, Guid projectId, ProjectBidPostDTO dto)
            => Ok(await MicroBus.Send(new UpdateProjectBidCommand(id, projectId, dto, GetDepartmentId())));

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpDelete("{id}/{projectId}")]
        public async Task<IActionResult> Delete(int id, Guid projectId)
            => Ok(await MicroBus.Send(new DeleteProjectBidCommand(id, projectId, GetDepartmentId())));
    }
}
