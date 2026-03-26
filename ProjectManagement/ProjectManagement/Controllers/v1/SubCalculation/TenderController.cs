using Application.Feature.Calculation.Tender.Commands;
using Application.Feature.Calculation.Tender.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Server.Controllers.v1.SubCalculation
{
    [ApiVersion("1.0")]
    public class TenderController : BaseApiController
    {
        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpGet(URLConst.Details + "/{id}/{calculationId}")]
        public async Task<IActionResult> Details(int id, int calculationId)
        {
            return Ok(await MicroBus.Send(new GetTenderDetailsQuery(id, calculationId)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpGet(URLConst.GetAll + "/{calculationId}")]
        public async Task<IActionResult> GetAll(int calculationId)
        {
            return Ok(await MicroBus.Send(new GetTenderListQuery(calculationId)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost("{calculationId}/{companyId}")]
        public async Task<IActionResult> Create(int calculationId, int companyId, TenderPostDTO dto)
        {
            return Ok(await MicroBus.Send(new CreateTenderCommand(dto, calculationId, companyId)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost(URLConst.Tender.Attribute + "/{calculationId}")]
        public async Task<IActionResult> Create(int calculationId, TenderAttributeListPostDTO dto)
        {
            return Ok(await MicroBus.Send(new CreateTenderAttributeCommand(dto, calculationId)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpGet(URLConst.Tender.Attribute + "/{TenderId}/{AttributeId}/{val}")]
        public async Task<IActionResult> UpdateBind(
            int TenderId,
            int AttributeId,
            [FromRoute(Name = "val")] decimal AttrValue)
        {
            return Ok(await MicroBus.Send(new UpdateTenderAttributeBindCommand(TenderId, AttributeId, AttrValue)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut("{id}/{calculationId}")]
        public async Task<IActionResult> Update(int id, int calculationId, TenderPostDTO dto)
        {
            return Ok(await MicroBus.Send(new UpdateTenderCommand(dto, id, calculationId)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut(URLConst.Tender.Attribute + "/{id}/{calculationId}")]
        public async Task<IActionResult> Update(int id, int calculationId, TenderAttributePostDTO dto)
        {
            return Ok(await MicroBus.Send(new UpdateTenderAttributeCommand(dto, id, calculationId)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpDelete("{id}/{calculationId}")]
        public async Task<IActionResult> Delete(int id, int calculationId)
        {
            return Ok(await MicroBus.Send(new DeleteTenderCommand(id, calculationId)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpDelete(URLConst.Tender.Attribute + "/{id}/{calculationId}")]
        public async Task<IActionResult> DeleteAttribute(int id, int calculationId)
        {
            return Ok(await MicroBus.Send(new DeleteTenderAttributeCommand(id, calculationId)));
        }
    }
}
