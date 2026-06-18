using Domain.Entities.Base;

namespace Domain.Entities.Users;

/// <summary>Tenant-scoped history of administrative changes made to users.</summary>
public sealed class UserManagementAuditEntity : BaseEntity<long>
{
    public string Action { get; private set; } = string.Empty;
    public int? TargetUserId { get; private set; }
    public string? TargetAuthId { get; private set; }
    public string TargetDisplayName { get; private set; } = string.Empty;
    public int? ActorUserId { get; private set; }
    public string ActorDisplayName { get; private set; } = string.Empty;
    public string? Details { get; private set; }
    public bool Succeeded { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private UserManagementAuditEntity() { }

    public static UserManagementAuditEntity Create(
        string action,
        int? targetUserId,
        string? targetAuthId,
        string? targetDisplayName,
        int? actorUserId,
        string? actorDisplayName,
        string? details,
        bool succeeded)
        => new()
        {
            Action = Normalize(action, 60, "unknown"),
            TargetUserId = targetUserId,
            TargetAuthId = NormalizeNullable(targetAuthId, 450),
            TargetDisplayName = Normalize(targetDisplayName, 240, "Unknown user"),
            ActorUserId = actorUserId,
            ActorDisplayName = Normalize(actorDisplayName, 240, "System"),
            Details = NormalizeNullable(details, 1000),
            Succeeded = succeeded,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

    private static string Normalize(string? value, int maxLength, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string? NormalizeNullable(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
