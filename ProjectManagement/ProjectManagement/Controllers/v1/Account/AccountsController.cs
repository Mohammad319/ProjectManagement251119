using Application.Feature.Account.Commands;
using Application.Feature.Account.Queries.AccountGroup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Account;
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
            return Ok((await MicroBus.Send(new GetAccountGroupsQuery())).Select(x => new
            {
                x.Id,
                x.Name,
                Accounts = x.Accounts.Select(a => new
                { a.Id, a.Name, a.IsVisible, a.Metadata, a.Code, })
            }));
        }
        [HttpGet]
        [Authorize(Roles = Tenant.Users)]
        public async Task<IActionResult> GetAccountGroupsAsList()
        {
            return Ok(await MicroBus.Send(new GetAccountGroupsAsListQuery()));
        }

        [Authorize(Roles = Tenant.AdminManger)]
        [HttpPost(URLConst.Account.AddRange)]
        public async Task<IActionResult> CreateRange(List<PostAccountGroupWithAccountsDTO> dto)
        {
            var command = new CreateRangeAccountGroupCommand(dto);
            return Ok(await MicroBus.Send(command));
        }
    }
}
