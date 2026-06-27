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
        [Authorize(Roles = PMRolesConst.Tenant.UsersAndViewer), HttpGet(URLConst.Folder.GetFoldersByDepartmentId + "/{id}")]
        public async Task<IActionResult> GetByDepartment(int id, bool includeArchived = false)
        {
            var myDepartmentId = GetDepartmentId();
            var isAdmin = User.IsInRole(PMRolesConst.Tenant.Admin);

            // Normal access: own department, or any department for Admin (tenant-wide).
            if (isAdmin || myDepartmentId == id)
                return Ok(await MicroBus.Send(new GetFoldersDepartmentQuery(includeArchived, id, GetUserId(), IsViewer())));

            // Shared department: allowed only when the user actually has shared/assigned projects there.
            // Folders are returned as read-only visual groups (IsSharedGroup = true); managing them stays
            // blocked in the mutation endpoints/services.
            if (await MicroBus.Send(new HasSharedProjectsInDepartmentQuery(id, GetUserId(), myDepartmentId)))
                return Ok(await MicroBus.Send(new GetSharedDepartmentFoldersQuery(id, GetUserId(), myDepartmentId, includeArchived)));

            return Forbid();
        }

        /// <summary>Departments the user may pick in the workspace dropdown (normal + shared-only with 👥).</summary>
        [Authorize(Roles = PMRolesConst.Tenant.UsersAndViewer), HttpGet(URLConst.Folder.AccessibleDepartments)]
        public async Task<IActionResult> GetAccessibleDepartments()
        {
            return Ok(await MicroBus.Send(new GetAccessibleDepartmentsQuery(GetUserId(), GetDepartmentId())));
        }

        /// <summary>"Alla tillgängliga": every folder the user can reach across departments.</summary>
        [Authorize(Roles = PMRolesConst.Tenant.UsersAndViewer), HttpGet(URLConst.Folder.AccessibleFolders)]
        public async Task<IActionResult> GetAccessibleFolders(bool includeArchived = false)
        {
            var isAdmin = User.IsInRole(PMRolesConst.Tenant.Admin);
            return Ok(await MicroBus.Send(new GetAccessibleFoldersQuery(GetUserId(), GetDepartmentId(), isAdmin, includeArchived)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.Users), HttpGet("getall")]
        public async Task<IActionResult> GetAll()
        {
            if (User.IsInRole(PMRolesConst.Tenant.Admin))
                return Ok(await MicroBus.Send(new GetAllFoldersQuery()));

            return Ok(await MicroBus.Send(new GetFoldersDepartmentQuery(false, GetDepartmentId(), GetUserId(), IsViewer())));
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
