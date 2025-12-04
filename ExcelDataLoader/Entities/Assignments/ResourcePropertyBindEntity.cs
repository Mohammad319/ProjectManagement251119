using ProjectImportHub.Entities;

namespace ProjectImportHub.Entities.Assignments
{
    public class ResourcePropertyBindEntity
    {
        public int Id { get; set; }
        public int ResourceId { get; set; }
        public ResourceEntity? Resource { get; set; }
        public int PropertyId { get; set; }
        public ResourcePropertyEntity? Property { get; set; }
        public string? TextDefault { get; set; }
        public double? NumberDefault { get; set; }
    }
}
