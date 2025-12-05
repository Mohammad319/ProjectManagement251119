using Application.Feature.Calculation.Resource.Commands;
using Application.Feature.Calculation.Storage.Commands;
using Application.Feature.Calculation.Storage.Queries;
using Application.Feature.Calculation.Task.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Persistence.Factory;
using ProjectImportHub.Entities;
using ProjectImportHub.Services.ProjectTask;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.Enums;
using System.Collections.Generic;

namespace ProjectManagement.Server.Controllers.v1.CalcExtension
{
    [ApiVersion("1.0")]
    public class StoragesController(IProjectTaskService TaskService) : BaseApiController
    {
        [Authorize]
        [HttpPost("updaterestenant/{resId}")]
        public async Task<IActionResult> UpdateResourceAppStorageTenantAsync(int resId,[FromBody] ResourceTenantLinkBase f)
        {
            int? tenantId = GetTenantId();
            if (!tenantId.HasValue) return BadRequest();
            return Ok(await TaskService.UpdateResourceAppStorageTenantAsync(tenantId.Value, resId, f, CancellationToken.None));
        }
        [Authorize]
        [HttpPost("tasksapp2")]
        public async Task<IActionResult> TasksApp2([FromBody] ProjectTaskFilterDto f)
        {
            int? tenantId = GetTenantId();
            List<ProjectTaskDto> _tasks = [];
            if (tenantId.HasValue)
            {
                    _tasks = await TaskService.GetTasksForUserDtoAsync(f, tenantId.Value, CancellationToken.None);
            }
            return Ok(_tasks);
        }
        [Authorize]
        [HttpGet("tasksapp2/{id}")]
        public async Task<IActionResult> TasksApp2(int id)
        {
            int? tenantId = GetTenantId();
            ProjectTaskDto _tasks = new();
            if (tenantId.HasValue)
            {
                    _tasks = await TaskService.GetTaskForUserDtoAsync(id, tenantId.Value, 0, CancellationToken.None);
            }
            return Ok(_tasks);
        }
        [Authorize]
        [HttpGet("tasksapp")]
        public async Task<IActionResult> TasksApp()
        {
            var tasks = await TaskService.GetTasksWithAdjustedResources(null, null, null, null);
            return Ok(tasks);
        }
        [Authorize]
        [HttpGet("resapp")]
        public Task<IActionResult> ResourcesApp()
        {
            List<ResourceDefinition>? resources = null;// await ResourceService.GetAllAsync();
            return Task.FromResult<IActionResult>(Ok(resources));
        }
        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpPost]
        public async Task<IActionResult> Index([FromBody] GetFilterDTO p)
        {
            if (p == null) return BadRequest();
            //if (p.AuthoritySelected == AuthorityStorage.program)
            //{
            //    if (p.QuestionKind == CalculationItemType.task)
            //        return Ok(await task.GetAsync(p.SortSelected, null, p.ItemCalcCategory));
            //    else if (p.QuestionKind == CalculationItemType.resource) return Ok(await groupRepo.GetVisibleAsync(p, true));
            //}
            return Ok(await MicroBus.Send(new GetStorageQuery(p.Type, p.AuthoritySelected, p.SortSelected)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpPost("cre")]
        public async Task<IActionResult> Create([FromBody] PostStorygeDTO post)
        {
            if (post.Type == CalculationItemType.resource)
            {
                if (post.copyType == CopyType.Move)
                    return Ok(await MicroBus.Send(new CutResourceCommand(post.Items, post.ParentID, post.OldCalcID)));
                if (post.copyType == CopyType.Copy)
                    return Ok(await MicroBus.Send(new CopyResourceCommand(post.Items, post.ParentID, post.OldCalcID)));
            }
            else if (post.Type == CalculationItemType.task)
            {
                if (post.copyType == CopyType.Move)
                    return Ok(await MicroBus.Send(new CutTaskCommand() { Items = post.Items, OldCalcId = post.OldCalcID, NewNetCalcId = post.NewCalcID, TaskParentID = post.ParentID, IsOH = post.IsOH }));
                if (post.copyType == CopyType.Copy)
                    return Ok(await MicroBus.Send(new CopyTaskCommand(post.Items, post.ParentID > 0 ? post.ParentID : null, post.OldCalcID, post.NewCalcID, post.IsOH)));
            }
            return BadRequest();
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpGet(URLConst.Storages.Save + "/{id}/{type}/{level}/{sort}")]
        public async Task<IActionResult> Save(int id, CalculationItemType type, AuthorityStorage level, StorageSort sort)
            => Ok(await MicroBus.Send(new CreateStorageCommand(type, level, sort, id, GetUserId(), GetDepartmentId())));

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpGet(URLConst.Storages.Remove + "/{id}")]
        public async Task<IActionResult> Remove(int id) =>
            Ok(await MicroBus.Send(new DeleteStorageCommand(id, GetUserId())));
    }
}
