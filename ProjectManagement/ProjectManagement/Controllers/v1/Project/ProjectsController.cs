using Application.Feature.Project.Project.Commands;
using Application.Feature.Project.Project.Queries;
using Application.Feature.Project.Type.Queries;
using Application.Feature.Transfer.Commands;
using ProjectManagement.Shared.DTO.Transfer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Project;
using System.Security.Claims;
using static ProjectManagement.Shared.Constant.PMRolesConst;

namespace ProjectManagement.Server.Controllers.v1
{
    [ApiVersion("1.0")]
    public class ProjectsController : BaseApiController
    {
        [Authorize(Roles = Tenant.Users)]
        [HttpGet("config/{m}/{con}/{com}/{t}/{st}/{proc?}")]
        public async Task<IActionResult> Config(int m, int con, int com, int t, int st, int proc = 0)
        {
            return Ok(await MicroBus.Send(new GetProjectCalcConfigQuery() { Methods = m, Contracts = con, Compensations = com, Types = t, ProjectStatuses = st, Procedures = proc, TypeObj = 0 }));
        }
        [Authorize(Roles = Tenant.UsersAndViewer)]
        [HttpGet(URLConst.Project.GetByFolderDepartmentId + "/{folderId}")]
        public async Task<IActionResult> GetByFolderId(Guid folderId, bool includeArchived = false)
        {
            return Ok(await MicroBus.Send(new GetProjectsGroupsByFolderQuery(folderId, includeArchived, GetUserId(), GetDepartmentId(), IsViewer())));
        }
        [Authorize(Roles = Tenant.UsersAndViewer)]
        [HttpGet(URLConst.Project.GetProjectsOtherDepartment + "/{folderId}")]
        public async Task<IActionResult> GetOtherDepartmentAsync(Guid folderId, bool includeArchived = false)
        {
            return Ok(await MicroBus.Send(new GetProjectsOtherGroupByFolderQuery(folderId, GetUserId(), GetDepartmentId(), includeArchived, IsViewer())));
        }
        [Authorize(Roles = Tenant.UsersAndViewer)]
        [HttpPost(URLConst.Project.Search)]
        public async Task<IActionResult> Filter(ProjectFilter dto)
        {
            var departmentId = GetDepartmentId();
            if (!departmentId.HasValue)
                return BadRequest("Department not found.");

            return Ok(await MicroBus.Send(new GetProjectsBySearchQuery(dto, GetUserId(), departmentId.Value, IsViewer())));
        }
        [Authorize(Roles = Tenant.Users)]
        [HttpGet(URLConst.Details + "/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            return Ok(await MicroBus.Send(new GetProjectDetailsQuery(id, GetUserId(), GetDepartmentId())));
        }
        [Authorize(Roles = Tenant.AdminManger)]
        [HttpGet(URLConst.Project.GetProjectPost + "/{id}")]
        public async Task<IActionResult> GetToPost(Guid id)
        {
            return Ok(await MicroBus.Send(new GetProjectPostQuery(id, GetUserId(), GetDepartmentId(), IsViewer())));
        }
        [Authorize(Roles = Tenant.AdminManger)]
        [HttpGet(URLConst.ReOrder + "/{Id}/{NewOrder}")]
        public async Task<IActionResult> ReOrder(Guid Id, int NewOrder)
        {
            return Ok(await MicroBus.Send(new NewOrderProjectCommand(Id, NewOrder, GetDepartmentId())));
        }
        [Authorize(Roles = Tenant.AdminManger)]
        [HttpPost]
        public async Task<IActionResult> Create(PostProjectDTO dto)
        {
            return Ok(await MicroBus.Send(new CreateProjectCommand(dto, GetUserId(), GetDepartmentId())));
        }

        [Authorize(Roles = Tenant.AdminManger)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, PostProjectDTO dto)
        {
            return Ok(await MicroBus.Send(new EditProjectCommand(id, dto, GetUserId(),GetDepartmentId())));
        }

        [Authorize(Roles = Tenant.AdminManger)]
        [HttpPut(URLConst.Project.Move + "/{id}/{targetFolderId}")]
        public async Task<IActionResult> Move(Guid id, Guid targetFolderId)
        {
            return Ok(await MicroBus.Send(new MoveProjectCommand(id, targetFolderId, GetUserId(), GetDepartmentId(), CanUseTargetDepartmentAccessAcrossDepartments())));
        }

        [Authorize(Roles = Tenant.AdminManger)]
        [HttpGet(URLConst.Project.Copy + "/{targetFolderId}/{id}")]
        public async Task<IActionResult> Copy(Guid targetFolderId, Guid id, bool includeCalculations = true)
        {
            return Ok(await MicroBus.Send(new CopyProjectCommand(id, targetFolderId, includeCalculations, GetUserId(), GetDepartmentId(), CanUseTargetDepartmentAccessAcrossDepartments())));
        }

        [Authorize(Roles = Tenant.AdminManger)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            return Ok(await MicroBus.Send(new DeleteProjectCommand( id, GetUserId(),  GetDepartmentId() )));
        }

        // ─────────── External project copy (ATACOST package) ───────────

        /// <summary>Creates a project copy (.atacost) with the selected calculations. Private calculations are excluded in the backend.</summary>
        [Authorize(Roles = Tenant.Users)]
        [HttpPost(URLConst.Project.ExportCopy + "/{id}")]
        public async Task<IActionResult> ExportCopy(Guid id, [FromBody] AtacostProjectExportRequest request)
        {
            var bytes = await MicroBus.Send(new BuildProjectPackageCommand(
                id, request ?? new(), GetUserId(), GetDepartmentId(), IsViewer()));

            if (bytes is null)
                return Forbid();

            return Ok(bytes);
        }

        /// <summary>Reads metadata from an uploaded .atacost package without importing.</summary>
        [Authorize(Roles = Tenant.Users)]
        [HttpPost(URLConst.Project.InspectCopy)]
        public async Task<IActionResult> InspectCopy([FromBody] byte[] fileBytes)
        {
            return Ok(await MicroBus.Send(new InspectPackageCommand(fileBytes)));
        }

        /// <summary>Dry-run of a project import: shows the import preview (matched values, deviations, target) without writing anything.</summary>
        [Authorize(Roles = Tenant.AdminManger)]
        [HttpPost(URLConst.Project.PreviewCopy + "/{targetFolderId}")]
        public async Task<IActionResult> PreviewCopy(Guid targetFolderId, [FromBody] AtacostImportRequest request)
        {
            return Ok(await MicroBus.Send(new PreviewProjectPackageCommand(request.FileBytes, targetFolderId, GetUserId(), request.Overrides)));
        }

        /// <summary>Imports a project copy into a target folder and always creates a new project.</summary>
        [Authorize(Roles = Tenant.AdminManger)]
        [HttpPost(URLConst.Project.ImportCopy + "/{targetFolderId}")]
        public async Task<IActionResult> ImportCopy(Guid targetFolderId, [FromBody] AtacostImportRequest request)
        {
            return Ok(await MicroBus.Send(new ImportProjectPackageCommand(
                request.FileBytes, targetFolderId, GetUserId(), GetDepartmentId(), CanUseTargetDepartmentAccessAcrossDepartments(), request.Overrides)));
        }

        private bool CanUseTargetDepartmentAccessAcrossDepartments() =>
            User.IsInRole(Tenant.Admin);
    }
}
