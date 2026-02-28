using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.ResourceType;
using ProjectManagement.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Calculation
{
    public class ResourceTypeBase
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsVisible { get; set; } = true;
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public ResourceTypesEnum Type { get; set; } = default!;
    }

    public class ResourceSortForm
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
        public int Order { get; set; }
    }
}
