using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Calculation
{
    public class ResourceData2
    {
        public List<string> UpperNote { get; set; } = [];
        public string QuantityParam { get; set; }
        public double? Cap { get; set; } = 0;
        public double? Waste { get; set; } = 0;
    }
    public class ResourceData
    {
        public string Note { get; set; }
        public List<string> UpperNote { get; set; } = [];
        public string QuantityParam { get; set; }
        public double? Quantity { get; set; }
        [MaxLength(25, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Unit { get; set; }
        public double ChangeFactor1 { get; set; } = 1;
        public double ChangeFactor2 { get; set; } = 1;

        public double CapWaste { get; set; }
        public double Cap { get; set; } = 0;
        public double Waste { get; set; } = 0;

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public double Cost { get; set; }
        public double? BaseCost { get; set; }
        public double? CO2 { get; set; }

        public ResourceData Clone()
        {
            return new ResourceData
            {
                UpperNote = [.. UpperNote],
                QuantityParam = QuantityParam,
                Cap = Cap,
                Waste = Waste
            };
        }
    }

    public class ResourceBase
    {
        public ResourceTypesEnum ResType { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; }
        public double Order { get; set; }
        public bool Active { get; set; } = true;
    }
}
