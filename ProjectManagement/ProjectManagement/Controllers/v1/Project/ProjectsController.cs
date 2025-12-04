using Application.Feature.Project.Project.Commands;
using Application.Feature.Project.Project.Queries;
using Application.Feature.Project.Type.Queries;
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
        [HttpGet("config/{m}/{con}/{com}/{t}")]
        public async Task<IActionResult> Config(int m, int con, int com, int t)
        {
            return Ok(await MicroBus.Send(new GetProjectCalcConfig() { Methods = m, Contracts = con, Compensations = com, Types = t, TypeObj = 0 }));
        }
        [Authorize(Roles = Tenant.Users)]
        [HttpGet(URLConst.Project.GetByFolderDepartmentId + "/{folderId}")]
        public async Task<IActionResult> GetByFolderId(Guid folderId, bool isVisible = false)
        {
            return Ok(await MicroBus.Send(new GetProjectsGroupsByFolderQuery(folderId, isVisible , GetUserId(), GetDepartmentId())));
        }
        [Authorize(Roles = Tenant.Users)]
        [HttpGet(URLConst.Project.GetProjectsOtherDepartment + "/{folderId}")]
        public async Task<IActionResult> GetOtherDepartmentAsync(Guid folderId)
        {
            return Ok(await MicroBus.Send(new GetProjectsOtherGroupByFolderQuery(folderId, GetUserId() ,GetDepartmentId())));
        }
        [Authorize(Roles = Tenant.Users)]
        [HttpPost(URLConst.Project.Search)]
        public async Task<IActionResult> Filter(ProjectFilter dto)
        {
            return Ok(await MicroBus.Send(new GetProjectsBySearchQuery(dto, GetUserId(),GetDepartmentId().Value)));
        }
        [Authorize(Roles = Tenant.Users)]
        [HttpGet(URLConst.Details + "/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            return Ok(await MicroBus.Send(new GetProjectDetailsQuery(id )));
        }
        [Authorize(Roles = Tenant.AdminManger)]
        [HttpGet(URLConst.Project.GetProjectPost + "/{id}")]
        public async Task<IActionResult> GetToPost(Guid id)
        {
            return Ok(await MicroBus.Send(new GetProjectPostQuery(id )));
        }
        [Authorize(Roles = Tenant.AdminManger)]
        [HttpGet(URLConst.ReOrder + "/{Id}/{NewOrder}")]
        public async Task<IActionResult> ReOrder(Guid Id, double NewOrder)
        {
            return Ok(await MicroBus.Send(new NewOrderProjectCommand(Id, NewOrder )));
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
            return Ok(await MicroBus.Send(new EditProjectCommand(dto,id,GetUserId(),GetDepartmentId())));
        }

        [Authorize(Roles = Tenant.AdminManger)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            return Ok(await MicroBus.Send(new DeleteProjectCommand( id, GetUserId(),  GetDepartmentId() )));
        }
    }
}
