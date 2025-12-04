namespace ProjectImportHub.Entities.Questions.Groups
{
    public class ResourceOptionItemEntity
    {
        public int Id { get; set; }

        public int ResourceChoiceGroupId { get; set; }
        public ResourceOptionGroupEntity ResourceOptionGroup { get; set; } = null!;

        public int ResourceId { get; set; }
        public ResourceEntity Resource { get; set; } = null!;
    }
}
