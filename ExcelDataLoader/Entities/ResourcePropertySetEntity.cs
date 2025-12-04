using ProjectImportHub.Entities.Assignments;
using ProjectManagement.Shared.DTO.ProjectAppStorage;

namespace ProjectImportHub.Entities
{

    public class ResourcePropertySetEntity
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public List<ResourcePropertyEntity> Properties { get; set; } = [];
    }
    public class ResourcePropertyEntity
    {
        public int Id { get; set; }
        public int PropertySetId { get; set; }
        public ResourcePropertySetEntity? PropertySet { get; set; }

        public string DisplayName { get; set; } = string.Empty;
        public bool IsUserEditable { get; set; } = true;
        public DataType DataType { get; set; } = DataType.Text;
        public string? DefaultTextValue { get; set; }
        public double? DefaultNumericValue { get; set; }
        public double? PropertyValues { get; set; }
        public double? MaxNumericValue { get; set; }
        public List<ResourcePropertyBindEntity> PropertiesBind { get; set; } = [];
    }

}
