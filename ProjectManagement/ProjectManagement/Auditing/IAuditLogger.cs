namespace ProjectManagement.Auditing;

public interface IAuditLogger
{
    Task WriteAsync(AuditLogEntry entry, CancellationToken ct = default);
}
