using ProjectManagement.Shared.DTO.ChangeLog;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Client.Shared.Helper
{
    /// <summary>
    /// Builds the Swedish "Senaste ändringar" tooltip for the change indicator. The action text is
    /// object-type aware (projekt/kalkyl). When no structured history exists it falls back to a simple
    /// "Senast ändrad / Ändrad av" block. Never emits null/undefined/technical ids.
    /// </summary>
    public static class ChangeLogTextFormatter
    {
        public const string Heading = "Senaste ändringar";

        public static string ActionText(ChangeAction action, bool isProject) => action switch
        {
            ChangeAction.Created => isProject ? "Skapade projektet" : "Skapade kalkylen",
            ChangeAction.Updated => isProject ? "Ändrade projektuppgifter" : "Ändrade kalkyluppgifter",
            ChangeAction.StatusChanged => isProject ? "Ändrade projektstatus" : "Ändrade kalkylstatus",
            ChangeAction.ReviewerChanged => "Ändrade granskare",
            ChangeAction.Shared => isProject ? "Delade projektet" : "Delade kalkylen",
            ChangeAction.Archived => isProject ? "Arkiverade projektet" : "Arkiverade kalkylen",
            ChangeAction.Restored => isProject ? "Återställde projektet" : "Återställde kalkylen",
            ChangeAction.Moved => isProject ? "Flyttade projektet" : "Flyttade kalkylen",
            ChangeAction.Copied => isProject ? "Kopierade projektet" : "Kopierade kalkylen",
            _ => isProject ? "Ändrade projektet" : "Ändrade kalkylen"
        };

        /// <summary>
        /// Tooltip text: heading + up to five "dd.MM.yyyy HH:mm · Namn · Åtgärd" lines. When the
        /// history list is empty, a fallback "Senast ändrad / Ändrad av" block is shown instead.
        /// </summary>
        public static string BuildTooltip(
            IReadOnlyList<ChangeLogItemDTO>? items,
            DateTime? fallbackUpdatedAt,
            string? fallbackUpdatedBy,
            bool isProject)
        {
            var lines = new List<string> { Heading };

            if (items is { Count: > 0 })
            {
                foreach (var item in items.OrderByDescending(x => x.CreatedAt).Take(5))
                {
                    var when = item.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
                    var parts = new List<string> { when };
                    if (!string.IsNullOrWhiteSpace(item.ActorName))
                        parts.Add(item.ActorName!.Trim());
                    parts.Add(ActionText(item.Action, isProject));
                    lines.Add("• " + string.Join(" · ", parts));
                }
                return string.Join("\n", lines);
            }

            // Fallback (no structured history): never show null/undefined/ids.
            if (fallbackUpdatedAt is { } when2)
                lines.Add($"Senast ändrad: {when2.ToLocalTime():dd.MM.yyyy HH:mm}");
            if (!string.IsNullOrWhiteSpace(fallbackUpdatedBy))
                lines.Add($"Ändrad av: {fallbackUpdatedBy!.Trim()}");

            return string.Join("\n", lines);
        }
    }
}
