using Application.Feature.Organisation.Organisation.Commands;
using Application.Feature.Organisation.Organisation.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Organisation;

namespace ProjectManagement.Server.Controllers.v1.Company
{
    [ApiVersion("1.0")]
    public class OrganisationsController : BaseApiController
    {
        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpGet(URLConst.GetAll + "/{groupId}")]
        public async Task<IActionResult> Get(int groupId, bool isVisible)
        {
            return Ok(await MicroBus.Send(new GetOrganisationsQuery(groupId, isVisible)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpGet(URLConst.Company.GetVisibleOrByID + "/{groupId}")]
        public async Task<IActionResult> GetVisibleOrIdAsync(int groupId)
        {
            return Ok(await MicroBus.Send(new GetVisibleOrIdQuery(groupId)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpGet(URLConst.Company.GetToPost + "/{id}")]
        public async Task<IActionResult> GetToPost(int id)
        {
            return Ok(await MicroBus.Send(new GetOrganisationToPostQuery(id)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpGet(URLConst.Company.GetById + "/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await MicroBus.Send(new GetOrganisationByIdQuery(id)));
        }
    }
}
