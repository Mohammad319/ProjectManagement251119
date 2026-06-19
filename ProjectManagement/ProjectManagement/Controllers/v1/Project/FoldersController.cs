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
        public async Task<IActionResult> GetByDepartment(int id, bool includeArchived = false)
        {
            return Ok(await MicroBus.Send(new GetFoldersFromOtherDepartmentQuery(id, includeArchived)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.Users), HttpGet("getall")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await MicroBus.Send(new GetAllFoldersQuery()));
        }
        [Authorize(Roles = PMRolesConst.Tenant.UsersAndViewer)]
        [HttpGet(URLConst.GetList)]
        public async Task<IActionResult> GetByVisible(bool includeArchived = false)
        {
            return Ok(await MicroBus.Send(new GetFoldersDepartmentQuery(includeArchived, GetDepartmentId(), GetUserId(), IsViewer())));
        }
        [Authorize(Roles = PMRolesConst.Tenant.Users), HttpGet(URLConst.Details + "/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            return Ok(await MicroBus.Send(new GetDetailsFoldersQuery(id, GetDepartmentId())));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpGet(URLConst.ReOrder + "/{Id}/{NewOrder}")]
        public async Task<IActionResult> ReOrder(Guid Id, int NewOrder)
        {
            return Ok(await MicroBus.Send(new NewOrderFolderCommand(Id, NewOrder, GetDepartmentId())));
        }

        [Authorize(Roles = PMRolesConst.Tenant.Manger)]
        [HttpPost]
        public async Task<IActionResult> Create(PostFolderDTO dto)
        {
            var departmentId = GetDepartmentId();
            if (departmentId is null) return BadRequest();
            return Ok(await MicroBus.Send(new CreateFolderCommand(dto, GetUserId(), departmentId.Value)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost(URLConst.Folder.CreateForDepartment + "/{departmentId:int}")]
        public async Task<IActionResult> CreateForDepartment(int departmentId, PostFolderDTO dto)
        {
            if (!CanUseTargetDepartment(departmentId))
                return Forbid();

            return Ok(await MicroBus.Send(new CreateFolderCommand(dto, GetUserId(), departmentId)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, PostFolderDTO dto)
        {
            return Ok(await MicroBus.Send(new UpdateFolderCommand( id, dto, GetUserId(), GetDepartmentId())));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut(URLConst.Folder.Move + "/{id}/{targetDepartmentId:int}")]
        public async Task<IActionResult> Move(Guid id, int targetDepartmentId)
        {
            if (!CanUseTargetDepartment(targetDepartmentId))
                return Forbid();

            return Ok(await MicroBus.Send(new MoveFolderCommand(id, targetDepartmentId, GetUserId(), GetDepartmentId())));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            return Ok(await MicroBus.Send(new DeleteFolderCommand(id, GetUserId(), GetDepartmentId())));
        }
    }
}
