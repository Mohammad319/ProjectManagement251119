namespace TaskResourceBlueprints.Dto.ProjectTask
{
    public sealed record ProjectTaskListItemDto(
        int Id,
        string? Code,
        string DisplayName,
        string? UnitCode,
        decimal? Quantity,
        bool IsActive
    )
    {
        public List<ResourceTaskIndexDto> ResourceTasks { get; set; } = [];
    };

    public sealed record ResourceTaskIndexDto(
        int AssignmentId,
        int ResourceId,
        string DisplayName,
        decimal Chf1,
        decimal Chf2,
        string Unit,
        bool IsActive)
    {
        // Backward-compatible alias for UI paths that still read Id as the resource id.
        public int Id => ResourceId;
    }
}
