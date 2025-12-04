using Application.Feature.Offer.Commands;
using Application.Feature.Offer.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Offer;

namespace ProjectManagement.Server.Controllers.v1.SubCalculation
{
    [ApiVersion("1.0")]
    public class OffersController : BaseApiController
    {
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpGet(URLConst.Offer.Set + "/{resID}/{offerID}")]
        public async Task<IActionResult> Set(int resID, int? offerID)
        {
            return Ok(await MicroBus.Send(new GetSetOfferQuery() { OfferId = offerID, ResourceId = resID }));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpGet(URLConst.Offer.ReCalc + "/{calcID}/{orgID}/{avg}")]
        public async Task<IActionResult> ReCalc(int calcID,int orgID,double avg)
        {
            return Ok(await MicroBus.Send(new CalcAvgOfferCommand(calcID,orgID, avg)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost(URLConst.Offer.Offers)]
        public async Task<IActionResult> Post(PostOfferDTO dto)
        {
            return Ok(await MicroBus.Send(new CreateOfferCommand(dto)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut(URLConst.Offer.Offers + "/{id}")]
        public async Task<IActionResult> Update(int id, PostOfferDTO dto)
        {
            return Ok(await MicroBus.Send(new UpdateOfferCommand(dto,id)));
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpDelete(URLConst.Offer.Offers + "/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            return Ok(await MicroBus.Send(new DeleteOfferCommand (id)));
        }
        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost(URLConst.Offer.Filter)]
        public async Task<IActionResult> Filter([FromBody] OfferFilterDTO filter)
        {
            return Ok(await MicroBus.Send(new GetOffersByFilterQuery(filter)));
        }
    }
}
