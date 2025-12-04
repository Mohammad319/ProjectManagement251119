using Application.Feature.Organisation.OrganisationCategory.Commands;
using Application.Feature.Organisation.OrganisationCategory.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Organisation;

namespace ProjectManagement.Server.Controllers.v1.Company
{
    [ApiVersion("1.0")]
    public class OrganisationsCategoryController : BaseApiController
    {
        [HttpGet, Authorize(Roles = PMRolesConst.Tenant.Users)]
        public async Task<IActionResult> GetCategories()
        {
            return Ok(await MicroBus.Send(new GetOrganisationCategoryQuery()));
        }
    }
}
