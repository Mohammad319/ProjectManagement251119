namespace ProjectManagement.Shared.Enums
{
    /// <summary>
    /// A coarse, human-meaningful action recorded in the change log for a project or calculation.
    /// Stored as <see cref="int"/>. The Swedish display text is resolved at render time (object-type
    /// aware) so the same action reads "Ändrade projektstatus"/"Ändrade kalkylstatus" etc.
    /// </summary>
    public enum ChangeAction
    {
        Created = 0,
        Updated = 1,
        StatusChanged = 2,
        ReviewerChanged = 3,
        Shared = 4,
        Archived = 5,
        Restored = 6,
        Moved = 7,
        Copied = 8
    }
}
