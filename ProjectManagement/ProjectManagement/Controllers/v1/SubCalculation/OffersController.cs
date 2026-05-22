using Application.Feature.Calculation.Offer.Commands;
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
            var result = await MicroBus.Send(new SetOfferCommand(resID, offerID, GetDepartmentId()));
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpGet(URLConst.Offer.ReCalc + "/{calcID}/{orgID}/{avg}")]
        public async Task<IActionResult> ReCalc(int calcID, int orgID, double avg)
        {
            var result = await MicroBus.Send(new CalcAvgOfferCommand(calcID, orgID, avg, GetDepartmentId()));
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost(URLConst.Offer.Offers)]
        public async Task<IActionResult> Post([FromBody] PostOfferDTO dto)
        {
            var result = await MicroBus.Send(new CreateOfferCommand(dto, GetDepartmentId()));
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPut(URLConst.Offer.Offers + "/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PostOfferDTO dto)
        {
            // Allow If-Match / ETag based concurrency (optional) without breaking body-based RowVersion
            TrySetRowVersionFromIfMatch(dto);

            var result = await MicroBus.Send(new UpdateOfferCommand(id, dto, GetDepartmentId()));
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpDelete(URLConst.Offer.Offers + "/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await MicroBus.Send(new DeleteOfferCommand(id, GetDepartmentId()));
            TrySetETag(result);
            return Ok(result);
        }

        [Authorize(Roles = PMRolesConst.Tenant.AdminManger)]
        [HttpPost(URLConst.Offer.Filter)]
        public async Task<IActionResult> Filter([FromBody] OfferFilterDTO filter)
        {
            var result = await MicroBus.Send(new GetOffersByFilterQuery(filter, GetDepartmentId()));
            TrySetETag(result);
            return Ok(result);
        }
    }
}
