
using Domain.Repository.AuthPermissions;
using Microsoft.AspNetCore.Mvc;

namespace ProjectManagement.Server.Controllers.v1.Identity
{
    [ApiVersion("1.0")]
    public class Initialize2022sController(IAuthRepository _userRepo, IConfiguration configuration) : BaseApiController
    {
        [HttpGet("[action]")]
        public async Task<ActionResult> StartUserRole()
        {
            try
            {
                string Email = configuration.GetSection("User:Email").Get<string>() ?? string.Empty;
                string Password = configuration.GetSection("User:Password").Get<string>() ?? string.Empty;
                if (string.IsNullOrEmpty(Email)) return BadRequest();
                if (string.IsNullOrEmpty(Password)) Password = Email;

                if (await _userRepo.Initialize(Email, Password))
                    return Ok();
                return Ok(false);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
