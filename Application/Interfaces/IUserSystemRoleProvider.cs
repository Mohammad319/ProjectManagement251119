namespace Application.Interfaces
{
    /// <summary>
    /// Resolves system (tenant) roles from the identity/auth store. Used to honour the rule that a
    /// system <b>Visare</b> (Viewer) only ever gets "Kan visa", even when a department is shared as
    /// "Kan ändra" – so notifications reflect the user's true effective level.
    /// </summary>
    public interface IUserSystemRoleProvider
    {
        /// <summary>
        /// Returns the subset of the given external auth ids whose user holds the system Viewer role.
        /// </summary>
        Task<IReadOnlySet<string>> GetViewerAuthIdsAsync(IEnumerable<string> externalAuthIds, CancellationToken ct = default);
    }
}
