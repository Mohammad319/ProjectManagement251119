using Application.Feature.Identity.Department.Queries;
using Domain.Repository.AuthPermissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Persistence.Factory;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Server.Controllers.v1.Identity
{
    [ApiVersion("1.0")]
    public class DepartmentsController(IAuthRepository dataAccess) : BaseApiController
    {
        [HttpGet, Authorize(Roles = PMRolesConst.Tenant.Users)]
        public async Task<IActionResult> GetAsListAll()
        {
            return Ok(await MicroBus.Send(new GetDepartmentsAsListQuery()));
        }

        [HttpGet(URLConst.Department.GetUsers + "/{did}"), Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        public async Task<IActionResult> GetUsers(int? did)
        {
            if (did == 0)
            {
                did = null;
                if (GetDepartmentId().HasValue) return BadRequest();
            }
            var tenantId = GetTenantId();  // من الـ Claims
            return Ok(await dataAccess.GetUsersAsync(tenantId, did));
        }
    }
}
