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
    [RequestSizeLimit(16 * 1024)]
    public IActionResult Post([FromBody] ClientLogEvent e)
    {
        if (string.IsNullOrWhiteSpace(e.Message) || e.Message.Length > 2000)
            return BadRequest();

        if (!string.IsNullOrWhiteSpace(e.Url) && e.Url.Length > 2000)
            return BadRequest();

        if (!string.IsNullOrWhiteSpace(e.TraceId) && e.TraceId.Length > 200)
            return BadRequest();

        if (!string.IsNullOrWhiteSpace(e.Exception) && e.Exception.Length > 8000)
            return BadRequest();

        var serverTraceId = HttpContext.TraceIdentifier;
        var isAuthenticated = User?.Identity?.IsAuthenticated == true;

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

        // Only trust identity claims when the caller is authenticated.
        var userId = isAuthenticated ? User?.FindFirst("UserId")?.Value : null;
        var tenantId = isAuthenticated ? User?.FindFirst("TenantID")?.Value : null;

        var logger = Log.ForContext("App", "ProjectManagement.Client")
            .ForContext("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
            .ForContext("ClientTraceId", e.TraceId)
            .ForContext("ServerTraceId", serverTraceId)
            .ForContext("ClientUrl", e.Url)
            .ForContext("ClientTimeUtc", e.ClientTimeUtc?.UtcDateTime)
            .ForContext("ClientUserId", userId)
            .ForContext("ClientTenantId", tenantId)
            .ForContext("ClientAuthenticated", isAuthenticated);

        if (string.IsNullOrWhiteSpace(e.Exception))
            logger.Write(level, "{ClientMessage}", e.Message);
        else
            logger.Write(level, "{ClientMessage} {ClientException}", e.Message, e.Exception);

        return Ok();
    }
}
