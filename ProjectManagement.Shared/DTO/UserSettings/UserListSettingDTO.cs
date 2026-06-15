namespace ProjectManagement.Shared.DTO.UserSettings
{
    /// <summary>
    /// A single personal list setting (visible columns, column widths, saved
    /// filters or saved column views) for the current user, identified by
    /// <see cref="Scope"/> + <see cref="Kind"/>. <see cref="Payload"/> is the JSON.
    /// </summary>
    public sealed class UserListSettingDTO
    {
        public string Scope { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }
}
