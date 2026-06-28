using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Shared.DTO.ChangeLog
{
    /// <summary>One change-log entry for the "Senaste ändringar" tooltip. The Swedish action text is
    /// resolved client-side (object-type aware); the DTO carries only the structured data.</summary>
    public sealed class ChangeLogItemDTO
    {
        public DateTime CreatedAt { get; set; }
        public string? ActorName { get; set; }
        public ChangeAction Action { get; set; }
    }
}
