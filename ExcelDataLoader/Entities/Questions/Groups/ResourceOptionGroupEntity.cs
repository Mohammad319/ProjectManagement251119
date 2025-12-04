namespace ProjectImportHub.Entities.Questions.Groups
{
    public class ResourceOptionGroupEntity
    {
        public int Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 0;
        public string? SectionKey { get; set; }
        public int TaskId { get; set; }
        public ProjectTaskEntity Task { get; set; } = null!;
        public List<ResourceOptionItemEntity> Items { get; set; } = [];
    }
}
