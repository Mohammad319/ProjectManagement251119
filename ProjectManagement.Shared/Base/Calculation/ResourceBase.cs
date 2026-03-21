using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.Base.Calculation
{
    public class ResourceParameter()
    {
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Value { get; set; } = 1;

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

        
        /// <summary>
        /// توحيد القيم الرقمية لتفادي أرقام طويلة جدًا (خصوصًا بعد الصيغ) + منع قيم سالبة في المال.
        /// </summary>
        public void Normalize()
        {
            // money
            Cost = RoundMoney(Cost);
            BaseCost = BaseCost.HasValue ? RoundMoney(BaseCost.Value) : null;
            PriceSub = PriceSub.HasValue ? RoundMoney(PriceSub.Value) : null;

            // quantity
            Quantity = Quantity.HasValue ? RoundQuantity(Quantity.Value) : null;

            // factors (cap/waste/change factors)
            ChangeFactor1 = RoundFactor(ChangeFactor1);
            ChangeFactor2 = RoundFactor(ChangeFactor2);
            CapWaste = RoundFactor(CapWaste);
            Cap = RoundFactor(Cap);
            Waste = RoundFactor(Waste);

            // clamp negatives where it doesn't make sense
            if (Cost < 0m) Cost = 0m;
            if (BaseCost.HasValue && BaseCost.Value < 0m) BaseCost = 0m;
            if (PriceSub.HasValue && PriceSub.Value < 0m) PriceSub = 0m;
            if (Quantity.HasValue && Quantity.Value < 0m) Quantity = 0m;

            // normalize times
            if (Times is not null)
            {
                foreach (var t in Times)
                {
                    t.Value = RoundFactor(t.Value);
                    t.Quantity = RoundQuantity(t.Quantity);
                    t.Cost = RoundMoney(t.Cost);
                }
            }
        }

        private static decimal RoundMoney(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        private static decimal RoundQuantity(decimal v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);
        private static decimal RoundFactor(decimal v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);

public ResourceMetadata Clone()
        {
            return new ResourceMetadata
            {
                // Deep copy لتفادي مشاركة نفس الـ reference بين النسخ
                Parameters = Parameters is null
                    ? new()
                    : [.. Parameters.Select(p => new ResourceParameter
                    {
                        Name = p.Name,
                        Unit = p.Unit,
                        Value = p.Value,
                    })],

                Times = Times is null
                    ? []
                    : [.. Times.Select(t => new ResourceTime
                    {
                        Name = t.Name,
                        Value = t.Value,
                        Quantity = t.Quantity,
                        Cost = t.Cost,
                    })],
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
        public int Order { get; set; }
        public bool Active { get; set; } = true;

        [JsonIgnore]
        public int SortOrder
        {
            get => Order;
            set => Order = value;
        }

        [JsonIgnore]
        public bool IsActive
        {
            get => Active;
            set => Active = value;
        }
    }
}
