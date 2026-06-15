using Application.Feature.Project.ProjectBid;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;

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

        // ─── Evaluation model (project-level) ────────────────────────────────

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut("{projectId}/evaluation-model/{model:int}")]
        public async Task<IActionResult> SetEvaluationModel(Guid projectId, int model)
            => Ok(await MicroBus.Send(new SetProjectBidEvaluationModelCommand(projectId, (BidEvaluationModel)model, GetDepartmentId())));

        // ─── Price columns (project-specific) ────────────────────────────────

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost("column/{projectId}")]
        public async Task<IActionResult> CreateColumn(Guid projectId, ProjectBidPriceColumnPostDTO dto)
            => Ok(await MicroBus.Send(new CreateProjectBidPriceColumnCommand(projectId, dto, GetDepartmentId())));

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut("column/{id}/{projectId}")]
        public async Task<IActionResult> RenameColumn(int id, Guid projectId, ProjectBidPriceColumnPostDTO dto)
            => Ok(await MicroBus.Send(new RenameProjectBidPriceColumnCommand(id, projectId, dto, GetDepartmentId())));

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpDelete("column/{id}/{projectId}")]
        public async Task<IActionResult> DeleteColumn(int id, Guid projectId)
            => Ok(await MicroBus.Send(new DeleteProjectBidPriceColumnCommand(id, projectId, GetDepartmentId())));

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut("column/{id}/{projectId}/move/{direction:int}")]
        public async Task<IActionResult> MoveColumn(int id, Guid projectId, int direction)
            => Ok(await MicroBus.Send(new MoveProjectBidPriceColumnCommand(id, projectId, direction, GetDepartmentId())));
    }
}
