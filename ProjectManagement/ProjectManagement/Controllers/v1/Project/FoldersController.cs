using Application.Feature.Project.Folder.Commands;
using Application.Feature.Project.Folder.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Folder;

namespace ProjectManagement.Server.Controllers.v1.Project
{
    [ApiVersion("1.0")]
    public class FoldersController : BaseApiController
    {
        [Authorize(Roles = PMRolesConst.Tenant.Users), HttpGet(URLConst.Folder.GetFoldersByDepartmentId + "/{id}")]
        public async Task<IActionResult> GetByDepartment(int id)
        {
            return Ok(await MicroBus.Send(new GetFoldersFromOtherDepartmentQuery(id)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.Users), HttpGet("getall")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await MicroBus.Send(new GetAllFoldersQuery()));
        }
        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpGet(URLConst.GetList)]
        public async Task<IActionResult> GetByVisible(bool isVisible = true)
        {
            return Ok(await MicroBus.Send(new GetFoldersDepartmentQuery(isVisible, GetDepartmentId())));
        }
        [Authorize(Roles = PMRolesConst.Tenant.Users), HttpGet(URLConst.Details + "/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            return Ok(await MicroBus.Send(new GetDetailsFoldersQuery(id)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpGet(URLConst.ReOrder + "/{Id}/{NewOrder}")]
        public async Task<IActionResult> ReOrder(Guid Id, double NewOrder)
        {
            return Ok(await MicroBus.Send(new NewOrderFolderCommand(Id, NewOrder)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.Super_Manger)]
        [HttpPost]
        public async Task<IActionResult> Create(PostFolderDTO dto)
        {
            var departmentId = GetDepartmentId();
            if (departmentId is null) return BadRequest();
            return Ok(await MicroBus.Send(new CreateFolderCommand(dto, GetUserId(), departmentId.Value)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, PostFolderDTO dto)
        {
            return Ok(await MicroBus.Send(new UpdateFolderCommand( id, dto, GetUserId(), GetDepartmentId())));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            return Ok(await MicroBus.Send(new DeleteFolderCommand(id, GetUserId(), GetDepartmentId())));
        }
    }
}
