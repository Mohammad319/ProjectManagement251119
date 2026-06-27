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
        // Read is allowed for view-only access too (Kan visa / Visare): the service returns a
        // read-only view (CanEdit=false) so shared users can open the evaluation.
        [Authorize(Roles = PMRolesConst.Tenant.UsersAndViewer)]
        [HttpGet("{projectId}")]
        public async Task<IActionResult> GetAll(Guid projectId)
            => Ok(await MicroBus.Send(new GetProjectBidsQuery(projectId, GetUserId(), GetDepartmentId(), IsViewer())));

        // ─── Anbudsjämförelse: anbud för flera projekt i en samlad query ─────
        [Authorize(Roles = PMRolesConst.Tenant.UsersAndViewer)]
        [HttpPost("comparison")]
        public async Task<IActionResult> Comparison([FromBody] List<Guid> projectIds)
            => Ok(await MicroBus.Send(new GetProjectBidComparisonQuery(projectIds ?? [], GetUserId(), GetDepartmentId(), IsViewer())));

        // Write endpoints keep the system-role gate; effective "Kan ändra" project access (incl.
        // share-as-Användare) is enforced in the service, which 403s view-only users with a clear message.
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost("{projectId}")]
        public async Task<IActionResult> Create(Guid projectId, ProjectBidPostDTO dto)
            => Ok(await MicroBus.Send(new CreateProjectBidCommand(projectId, dto, GetUserId(), GetDepartmentId())));

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut("{id}/{projectId}")]
        public async Task<IActionResult> Update(int id, Guid projectId, ProjectBidPostDTO dto)
            => Ok(await MicroBus.Send(new UpdateProjectBidCommand(id, projectId, dto, GetUserId(), GetDepartmentId())));

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpDelete("{id}/{projectId}")]
        public async Task<IActionResult> Delete(int id, Guid projectId)
            => Ok(await MicroBus.Send(new DeleteProjectBidCommand(id, projectId, GetUserId(), GetDepartmentId())));

        // ─── Evaluation model (project-level) ────────────────────────────────

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut("{projectId}/evaluation/{basis:int}/{method:int}")]
        public async Task<IActionResult> SetEvaluation(Guid projectId, int basis, int method)
            => Ok(await MicroBus.Send(new SetProjectBidEvaluationCommand(
                projectId, (BidEvaluationBasis)basis, (BidEvaluationModel)method, GetUserId(), GetDepartmentId())));

        // ─── Price columns (project-specific) ────────────────────────────────

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost("column/{projectId}")]
        public async Task<IActionResult> CreateColumn(Guid projectId, ProjectBidPriceColumnPostDTO dto)
            => Ok(await MicroBus.Send(new CreateProjectBidPriceColumnCommand(projectId, dto, GetUserId(), GetDepartmentId())));

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut("column/{id}/{projectId}")]
        public async Task<IActionResult> RenameColumn(int id, Guid projectId, ProjectBidPriceColumnPostDTO dto)
            => Ok(await MicroBus.Send(new RenameProjectBidPriceColumnCommand(id, projectId, dto, GetUserId(), GetDepartmentId())));

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpDelete("column/{id}/{projectId}")]
        public async Task<IActionResult> DeleteColumn(int id, Guid projectId)
            => Ok(await MicroBus.Send(new DeleteProjectBidPriceColumnCommand(id, projectId, GetUserId(), GetDepartmentId())));

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut("column/{id}/{projectId}/move/{direction:int}")]
        public async Task<IActionResult> MoveColumn(int id, Guid projectId, int direction)
            => Ok(await MicroBus.Send(new MoveProjectBidPriceColumnCommand(id, projectId, direction, GetUserId(), GetDepartmentId())));
    }
}
