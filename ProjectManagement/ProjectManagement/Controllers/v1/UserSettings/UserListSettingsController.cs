using Application.Feature.General.ListSettings.Commands;
using Application.Feature.General.ListSettings.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.UserSettings;

namespace ProjectManagement.Server.Controllers.v1.UserSettings
{
    [ApiVersion("1.0")]
    public class UserListSettingsController : BaseApiController
    {
        /// <summary>Returns all personal list settings for the current user in a scope.</summary>
        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string scope)
        {
            if (string.IsNullOrWhiteSpace(scope))
                return BadRequest();

            return Ok(await MicroBus.Send(new GetUserListSettingsQuery(GetUserId(), scope)));
        }

        /// <summary>Creates or replaces a single personal list setting for the current user.</summary>
        [Authorize(Roles = PMRolesConst.Tenant.Users)]
        [HttpPut]
        public async Task<IActionResult> Upsert([FromBody] UserListSettingDTO dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.Scope) || string.IsNullOrWhiteSpace(dto.Kind))
                return BadRequest();

            return Ok(await MicroBus.Send(new UpsertUserListSettingCommand(GetUserId(), dto.Scope, dto.Kind, dto.Payload)));
        }
    }
}
