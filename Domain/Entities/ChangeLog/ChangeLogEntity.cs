using Domain.Entities.Base;
using ProjectManagement.Shared.Enums;

namespace Domain.Entities.ChangeLog
{
    /// <summary>
    /// One recorded change to a project or calculation (lifecycle/status/sharing level, not field-level).
    /// Feeds the list/tree "ändringsindikator" tooltip ("Senaste ändringar"). Tenant-scoped via the global
    /// filter; the actor is the audit <c>CreatedBy</c> and <see cref="ActorName"/> is a denormalized snapshot
    /// so the tooltip never has to resolve user ids at read time.
    /// <para>Exactly one of <see cref="ProjectId"/> / <see cref="CalculationId"/> is set per row.</para>
    /// </summary>
    public sealed class ChangeLogEntity : AuditableEntity<long>
    {
        /// <summary>Related project (set for project changes; also set for calc changes for batch lookups is NOT done — calc uses CalculationId).</summary>
        public Guid? ProjectId { get; private set; }

        /// <summary>Related calculation (set for calculation changes).</summary>
        public int? CalculationId { get; private set; }

        public ChangeAction Action { get; private set; }

        /// <summary>Display name of the actor who made the change (snapshot). Null when unknown.</summary>
        public string? ActorName { get; private set; }

        private ChangeLogEntity() { }

        public static ChangeLogEntity ForProject(Guid projectId, ChangeAction action, string? actorName)
            => new()
            {
                ProjectId = projectId,
                Action = action,
                ActorName = Trim(actorName, 200)
            };

        public static ChangeLogEntity ForCalculation(int calculationId, ChangeAction action, string? actorName)
            => new()
            {
                CalculationId = calculationId,
                Action = action,
                ActorName = Trim(actorName, 200)
            };

        private static string? Trim(string? value, int maxLength)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return null;
            return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
        }
    }
}
