namespace ProjectManagement.Client.Shared.SharedComponent
{
    /// <summary>
    /// One option for <see cref="MhdFormSelect{TValue}"/>.
    /// <paramref name="Color"/> is only used when the dropdown renders a filled
    /// status dot (e.g. project status); it comes from the shared status model.
    /// </summary>
    public sealed class MhdSelectItem<TValue>
    {
        public TValue Value { get; init; } = default!;
        public string Label { get; init; } = string.Empty;
        public string? Color { get; init; }
    }
}
