namespace ProjectManagement.Auditing;

public sealed class SerilogAuditLogger(ILogger<SerilogAuditLogger> logger) : IAuditLogger
{
    public Task WriteAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Audit event {AuditEvent} {AuditAction} {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms. UserId={UserId} TenantId={TenantId} DepartmentId={DepartmentId} TraceId={TraceId} RemoteIp={RemoteIp}",
            entry.EventName,
            entry.Action,
            entry.Method,
            entry.Path,
            entry.StatusCode,
            entry.ElapsedMilliseconds,
            entry.UserId,
            entry.TenantId,
            entry.DepartmentId,
            entry.TraceId,
            entry.RemoteIp);

        return Task.CompletedTask;
    }
}
