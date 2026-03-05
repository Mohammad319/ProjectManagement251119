using System;

namespace ProjectManagement.Shared.Exceptions
{
    /// <summary>
    /// Thrown when an entity update fails due to optimistic concurrency (rowversion mismatch).
    /// Typically means the user edited stale data. The UI should reload and let the user retry.
    /// </summary>
    public sealed class ConcurrencyConflictException : Exception
    {
        public string EntityName { get; }
        public int EntityId { get; }

        public ConcurrencyConflictException(string entityName, int entityId)
            : base($"The {entityName} (Id={entityId}) was modified by someone else. Please reload and try again.")
        {
            EntityName = entityName;
            EntityId = entityId;
        }
    }
}
