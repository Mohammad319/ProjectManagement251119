namespace TaskResourceBlueprints.Dto.ProjectTask
{
    public sealed record ProjectTaskListItemDto(
        int Id,
        string? Code,
        string DisplayName,
        string? UnitCode,
        double? Quantity,
        bool IsActive
    )
    {
        public List<ResourceTaskIndexDto> ResourceTasks { get; set; } = [];
    };

    public sealed record ResourceTaskIndexDto(
    int Id,
    string DisplayName,
        decimal Chf1,
    decimal Chf2,
    string Unit,
    bool IsActive
);
}
