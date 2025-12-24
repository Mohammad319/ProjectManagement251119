using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.DTO.General;
using Serilog;
using Serilog.Events;

[AllowAnonymous]
[ApiController]
[Route("api/client-logs")]
public class ClientLogsController : ControllerBase
{
    [HttpPost]
    public IActionResult Post([FromBody] ClientLogEvent e)
    {
        if (string.IsNullOrWhiteSpace(e.Message) || e.Message.Length > 2000)
            return BadRequest();

        var serverTraceId = HttpContext.TraceIdentifier;

        var level = e.Level?.ToLowerInvariant() switch
        {
            "fatal" => LogEventLevel.Fatal,
            "error" => LogEventLevel.Error,
            "warning" => LogEventLevel.Warning,
            "information" => LogEventLevel.Information,
            "debug" => LogEventLevel.Debug,
            "verbose" => LogEventLevel.Verbose,
            _ => LogEventLevel.Error
        };

        // اختياري: خذها من claims إن وجدت
        var userId = User?.FindFirst("UserId")?.Value ?? e.UserId;
        var tenantId = User?.FindFirst("TenantID")?.Value ?? e.TenantId;

        Log.ForContext("App", "ProjectManagement.Client")
           .ForContext("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
           .ForContext("ClientTraceId", e.TraceId)
           .ForContext("ServerTraceId", serverTraceId)
           .ForContext("ClientUrl", e.Url)
           .ForContext("ClientTimeUtc", e.ClientTimeUtc?.UtcDateTime)
           .ForContext("ClientUserId", userId)
           .ForContext("ClientTenantId", tenantId)
           .Write(level, "{ClientMessage} {ClientException}", e.Message, e.Exception);

        return Ok();
    }
}
