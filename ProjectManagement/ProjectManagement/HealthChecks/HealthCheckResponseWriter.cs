using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace ProjectManagement.HealthChecks;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            timestampUtc = DateTimeOffset.UtcNow,
            entries = report.Entries.ToDictionary(
                pair => pair.Key,
                pair => new
                {
                    status = pair.Value.Status.ToString(),
                    description = pair.Value.Description,
                    durationMs = pair.Value.Duration.TotalMilliseconds,
                    tags = pair.Value.Tags,
                    error = pair.Value.Exception?.Message
                })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
