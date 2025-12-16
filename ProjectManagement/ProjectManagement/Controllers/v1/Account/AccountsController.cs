using Application.Feature.Account.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using static ProjectManagement.Shared.Constant.PMRolesConst;

namespace ProjectManagement.Server.Controllers.v1.Account
{
    [ApiVersion("1.0")]
    public class AccountsController : BaseApiController
    {
        [HttpGet(URLConst.Account.GetAll)]
        [Authorize(Roles = Tenant.Users)]
        public async Task<IActionResult> GetAll()
        {
            return Ok((await MicroBus.Send(new GetAccountGroupsAsListQuery())).Select(x => new
            {
                x.Id,
                x.Name,
                Accounts = x.Accounts.Select(a => new
                { a.Id, a.Name, a.Account, })
            }));
        }
        [HttpGet]
        [Authorize(Roles = Tenant.Users)]
        public async Task<IActionResult> GetAccountGroupsAsList()
        {
            return Ok(await MicroBus.Send(new GetAccountGroupsAsListQuery()));
        }
    }
}
