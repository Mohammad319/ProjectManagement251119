global using Asp.Versioning;
using Application.Interfaces;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Shared.Constant;
using System.Security.Claims;

namespace ProjectManagement.Server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class BaseApiController : ControllerBase
    {
        protected int? GetTenantId()
        {
            var tenantIdClaim = User?.FindFirst(PMClaimsConst.Tenant)?.Value;
            if (int.TryParse(tenantIdClaim, out var tenantId))
            {
                return tenantId;
            }
            return null;
        }
        protected int? GetDepartmentId()
        {
            if (int.TryParse(User.Claims.FirstOrDefault(x => x.Type == PMClaimsConst.DepartmentId)?.Value, out int dId))
            {
                return dId;
            }
            return null;
        }

        protected int GetUserId()
        {
            if (int.TryParse(User.Claims.FirstOrDefault(x => x.Type == PMClaimsConst.UserId)?.Value, out int dId))
            {
                return dId;
            }
            return 0;
        }
        private ICommandDispatcher? _dispatcher;
        protected ICommandDispatcher MicroBus => _dispatcher ??= HttpContext.RequestServices.GetRequiredService<ICommandDispatcher>();

        //private IMediator _mediator;
        //protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetService<IMediator>();
    }
}
