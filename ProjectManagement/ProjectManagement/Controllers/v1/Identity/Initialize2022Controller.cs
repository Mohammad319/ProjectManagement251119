using Domain.Repository.AuthPermissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectManagement.Server.Controllers.v1.Identity
{
    [ApiVersion("1.0")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class Initialize2022sController(
        IAuthRepository userRepo,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<Initialize2022sController> logger) : BaseApiController
    {
        [AllowAnonymous]
        [HttpPost("[action]")]
        public async Task<ActionResult<bool>> StartUserRole()
        {
            // هذا endpoint مخصص فقط لتهيئة أولية استثنائية.
            // لا يتم تفعيله إلا في Development أو عند تفعيل explicit flag.
            var explicitlyEnabled = configuration.GetValue<bool>("Bootstrap:EnableInitialize2022");
            if (!environment.IsDevelopment() && !explicitlyEnabled)
            {
                return NotFound();
            }

            var email = configuration["User:Email"];
            var password = configuration["User:Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return BadRequest("Bootstrap credentials are missing.");
            }

            try
            {
                var initialized = await userRepo.Initialize(email, password);
                return Ok(initialized);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bootstrap initialization failed for {Email}", email);
                return Problem(title: "Initialization failed.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
