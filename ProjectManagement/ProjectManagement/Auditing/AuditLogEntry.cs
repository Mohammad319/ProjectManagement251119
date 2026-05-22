namespace ProjectManagement.Auditing;

public sealed record AuditLogEntry(
    string EventName,
    string Action,
    string Method,
    string Path,
    int StatusCode,
    string? TraceId,
    string? UserId,
    string? TenantId,
    string? DepartmentId,
    string? RemoteIp,
    long ElapsedMilliseconds,
    DateTimeOffset TimestampUtc);
