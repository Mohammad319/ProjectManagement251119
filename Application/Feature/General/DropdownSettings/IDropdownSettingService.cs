namespace Application.Feature.General.DropdownSettings
{
    /// <summary>
    /// Per-tenant "Obligatorisk" setting for the admin-settings dropdown categories.
    /// The flag lives on the category/tab itself, never on individual list values.
    /// </summary>
    public interface IDropdownSettingService
    {
        /// <summary>
        /// Returns IsRequired for every known category (stored overrides merged onto the defaults).
        /// </summary>
        Task<Dictionary<string, bool>> GetRequirementsAsync(CancellationToken ct = default);

        Task SetRequiredAsync(string category, bool isRequired, CancellationToken ct = default);
    }
}
