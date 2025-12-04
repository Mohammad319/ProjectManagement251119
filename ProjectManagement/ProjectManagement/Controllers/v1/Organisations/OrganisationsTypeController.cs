using Application.Feature.Organisation.OrganisationType.Commands;
using Application.Feature.Organisation.OrganisationType.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Organisation;

namespace ProjectManagement.Server.Controllers.v1.Organisations
{
    [ApiVersion("1.0")]
    public class OrganisationsTypeController : BaseApiController
    {
        [Authorize(Roles = PMRolesConst.Tenant.Users), HttpGet(URLConst.GetAll)]
        public async Task<IActionResult> GetAll(bool isVisible)
        {
            return Ok(await MicroBus.Send(new GetAllOrganisationsTypeQuery(isVisible)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.Users), HttpGet(URLConst.GetList)]
        public async Task<IActionResult> GetAsList(int? id, int? cus)
        {
            return Ok(await MicroBus.Send(new GetListOrganisationsTypeQuery(id,cus)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminSuperManger), HttpPost]
        public async Task<IActionResult> Create(PostOrganisationTypeDTO dto)
        {
            return Ok(await MicroBus.Send(new CreateOrganisationTypeCommand(dto)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminSuperManger)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, PostOrganisationTypeDTO dto)
        {
            return Ok(await MicroBus.Send(new UpdateOrganisationTypeCommand(dto,id)));
        }
    }
}
