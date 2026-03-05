using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Calculation
{
    public class ResourceParameter()
    {
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public double Value { get; set; } = 1;

    }
    public class ResourceTime()
    {
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; } = 1;
        public decimal Quantity { get; set; } = 1;
        public decimal Cost { get; set; } = 1;

    }
    public class ResourceMetadata
    {
        public List<ResourceParameter> Parameters { get; set; } = [];
        public List<ResourceTime> Times { get; set; } = [];
        public decimal? PriceSub { get; set; }

        public string Note { get; set; } = string.Empty;
        public List<string> UpperNote { get; set; } = [];
        public string QuantityParam { get; set; } = string.Empty;
        public decimal? Quantity { get; set; }
        [MaxLength(25, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Unit { get; set; } = string.Empty;
        public decimal ChangeFactor1 { get; set; } = 1;
        public decimal ChangeFactor2 { get; set; } = 1;

        public decimal CapWaste { get; set; }
        public decimal Cap { get; set; } = 0;
        public decimal Waste { get; set; } = 0;

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public decimal Cost { get; set; }
        public decimal? BaseCost { get; set; }
        public double? CO2 { get; set; }

        public ResourceMetadata Clone()
        {
            return new ResourceMetadata
            {
                // Deep copy لتفادي مشاركة نفس الـ reference بين النسخ
                Parameters = Parameters is null
                    ? new()
                    : Parameters.Select(p => new ResourceParameter
                    {
                        Name = p.Name,
                        Unit = p.Unit,
                        Value = p.Value,
                    }).ToList(),

                Times = Times is null
                    ? new()
                    : Times.Select(t => new ResourceTime
                    {
                        Name = t.Name,
                        Value = t.Value,
                        Quantity = t.Quantity,
                        Cost = t.Cost,
                    }).ToList(),
                PriceSub = PriceSub,

                Note = Note ?? string.Empty,
                UpperNote = UpperNote is null ? new() : [.. UpperNote],

                QuantityParam = QuantityParam ?? string.Empty,
                Quantity = Quantity,
                Unit = Unit ?? string.Empty,

                ChangeFactor1 = ChangeFactor1,
                ChangeFactor2 = ChangeFactor2,

                CapWaste = CapWaste,
                Cap = Cap,
                Waste = Waste,

                Cost = Cost,
                BaseCost = BaseCost,
                CO2 = CO2,
            };
        }

    }

    public class ResourceBase
    {
        public ResourceTypesEnum ResType { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        public double Order { get; set; }
        public bool Active { get; set; } = true;
    }
}
