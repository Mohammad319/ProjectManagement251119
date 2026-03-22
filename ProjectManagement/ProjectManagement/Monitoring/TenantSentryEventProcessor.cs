using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Services;
using Sentry;
using Sentry.Extensibility;
using System.Globalization;

namespace ProjectManagement.Monitoring;

public sealed class TenantSentryEventProcessor(IHttpContextAccessor httpContextAccessor) : ISentryEventProcessor
{
    public SentryEvent Process(SentryEvent @event)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
            return @event;

        var tenantContext = httpContext.RequestServices.GetService<TenantContext>();

        if (tenantContext?.TenantId > 0)
            @event.SetTag("tenant.id", tenantContext.TenantId.ToString(CultureInfo.InvariantCulture));

        if (tenantContext?.DepartmentId is > 0)
            @event.SetTag("department.id", tenantContext.DepartmentId.Value.ToString(CultureInfo.InvariantCulture));

        if (tenantContext?.UserId is > 0)
            @event.SetTag("user.local_id", tenantContext.UserId.Value.ToString(CultureInfo.InvariantCulture));

        if (!string.IsNullOrWhiteSpace(httpContext.TraceIdentifier))
            @event.SetTag("server.trace_id", httpContext.TraceIdentifier);

        var requestPath = httpContext.Request.Path.Value;
        if (!string.IsNullOrWhiteSpace(requestPath))
            @event.SetTag("request.path", requestPath);

        return @event;
    }
}
