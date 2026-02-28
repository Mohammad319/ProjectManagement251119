using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.DTO.ResourceType
{
    public class ResourceTypeData
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public double Cost { get; set; } = 1;
        [MaxLength(25, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Unit { get; set; } = string.Empty;
        public double? FixedQ { get; set; }
        public double ChangeFactor1 { get; set; } = 1;
        public double ChangeFactor2 { get; set; } = 1;
        public double? BaseCost { get; set; }
        public double CapWaste { get; set; }
        public double? CO2 { get; set; }
    }
    public sealed class ResourceSortModel : ResourceTypeBaseData
    {
        public int Id { get; set; }//AccountId
        public int ResourceTypeId { get; set; }
        public int? AccountId { get; set; }
    }
    public class ResourceTypeBaseData: ResourceTypeData
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsVisible { get; set; } = true;

    }
    public class ResourceTypeModel : ResourceTypeBaseData
    {
        public ResourceTypesEnum Type { get; set; } = default!;
        public int Id { get; set; }
        public int? AccountId { get; set; }
    }
    public class PostResourceTypeDTO : ResourceTypeBaseData
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public ResourceTypesEnum Type { get; set; } = default!;
        public int? AccountId { get; set; }
    }
    public class ListResourceTypeDTO : ResourceTypeBaseData
    {
        public int Id { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public ResourceTypesEnum Type { get; set; } = default!;

        public int? AccountId { get; set; }
        public List<ListResourceSortDTO> ResourcesSort { get; set; } = [];
    }

    public class PostResourceSortDTO : ResourceTypeBaseData
    {
        public int? AccountId { get; set; }
        public int ResourceTypeId { get; set; }
    }

    public class ListResourceSortDTO : ResourceTypeBaseData
    {
        public int Id { get; set; }
        public int? AccountId { get; set; }

    }
}
