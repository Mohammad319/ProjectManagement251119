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
        public bool IsActive { get; set; } = true;
    }
    public enum QuantityResourceAddon
    {
        Multiplication = 1,
        Division = 2,
    }
    public class ResourceAddon()
    {
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public QuantityResourceAddon Type { get; set; } = QuantityResourceAddon.Multiplication;
        public decimal Factor { get; set; } = 1;

        public decimal Quantity(decimal resourceQ) => Type == QuantityResourceAddon.Multiplication ?
            resourceQ * Factor: resourceQ / Factor;
        public decimal Cost { get; set; }
        public decimal BaseCost { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ResourceTime()
    {
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal? Percentage { get; set; }
        [JsonInclude]
        public decimal Quantity { get; private set; } = 1;
        public decimal Cost { get; set; } = 1;
        public bool IsActive { get; set; } = true;

        public ResourceTime SetResolvedQuantity(decimal quantity)
        {
            Quantity = quantity;
            return this;
        }
    }
    public class ResourceMetadata
    {
        public int SchemaVersion { get; set; } = 1;

        public List<ResourceParameter> Parameters { get; set; } = [];
        public List<ResourceAddon> AddOns { get; set; } = [];
        public List<ResourceTime> Times { get; set; } = [];
        public decimal? PriceSub { get; set; }

        public string Note { get; set; } = string.Empty;
        public List<string> UpperNote { get; set; } = [];
        public string QuantityParam { get; set; } = string.Empty;
        public decimal ChangeFactor1 { get; set; } = 1;
        public decimal ChangeFactor2 { get; set; } = 1;

        public decimal CapWaste { get; set; }
        public bool CapFromTask { get; set; }
        public decimal Cap { get; set; } = 0;
        public decimal Waste { get; set; } = 0;

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public decimal Cost { get; set; }
        public decimal? BaseCost { get; set; }
        public double? CO2 { get; set; }
        public string ImportInfo { get; set; } = string.Empty;

        /// <summary>
        /// توحيد القيم الرقمية لتفادي أرقام طويلة جدًا (خصوصًا بعد الصيغ) + منع قيم سالبة في المال.
        /// </summary>
        public void Normalize()
        {
            // money
            Cost = RoundMoney(Cost);
            BaseCost = BaseCost.HasValue ? RoundMoney(BaseCost.Value) : null;
            PriceSub = PriceSub.HasValue ? RoundMoney(PriceSub.Value) : null;

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

            if (AddOns is not null)
            {
                foreach (var addOn in AddOns)
                {
                    addOn.Name = NormalizeText(addOn.Name);
                    addOn.Unit = NormalizeText(addOn.Unit);
                    //addOn.Quantity = RoundQuantity(addOn.Quantity);
                    addOn.Factor = addOn.Factor;
                    addOn.Type = addOn.Type;
                    addOn.Cost = RoundMoney(addOn.Cost);
                    addOn.BaseCost = RoundMoney(addOn.BaseCost);

                    //if (addOn.Quantity < 0m) addOn.Quantity = 0m;
                    if (addOn.Cost < 0m) addOn.Cost = 0m;
                    if (addOn.BaseCost < 0m) addOn.BaseCost = 0m;
                }
            }

            SyncTimesWithQuantity(null);
        }

        public void SyncTimesWithQuantity(decimal? quantity)
        {
            if (Times is null)
                return;

            var resourceQuantity = quantity ?? 0m;

            for (int i = 0; i < Times.Count; i++)
            {
                var t = Times[i];
                t.Name = NormalizeText(t.Name);
                t.Cost = RoundMoney(t.Cost);
                t.Percentage = t.Percentage.HasValue ? RoundFactor(t.Percentage.Value) : null;

                if (t.Percentage.HasValue && t.Percentage.Value < 0m)
                    t.Percentage = 0m;

                var normalizedQuantity = RoundQuantity(t.Quantity);
                if (normalizedQuantity < 0m)
                    normalizedQuantity = 0m;

                t.SetResolvedQuantity(normalizedQuantity);
            }

            if (Times.Count == 0)
                return;

            if (resourceQuantity > 0m)
            {
                InitializeMissingTimePercentages(resourceQuantity);

                if (Times.Any(x => x.Percentage.HasValue))
                    ApplyTimePercentages(resourceQuantity);

                return;
            }

            if (Times.Any(x => x.Percentage.HasValue))
            {
                for (int i = 0; i < Times.Count; i++)
                    Times[i].SetResolvedQuantity(0m);
            }
        }

        private void InitializeMissingTimePercentages(decimal resourceQuantity)
        {
            if (resourceQuantity <= 0m || Times is null)
                return;

            for (int i = 0; i < Times.Count; i++)
            {
                var time = Times[i];
                if (time.Percentage.HasValue)
                    continue;

                time.Percentage = RoundFactor((time.Quantity / resourceQuantity) * 100m);
            }
        }

        private void ApplyTimePercentages(decimal resourceQuantity)
        {
            if (Times is null || Times.Count == 0)
                return;

            var totalPercentage = RoundFactor(Times.Sum(x => x.Percentage ?? 0m));
            var balanceLastItem = totalPercentage == 100m;
            decimal assignedQuantity = 0m;

            for (int i = 0; i < Times.Count; i++)
            {
                var percentage = Times[i].Percentage ?? 0m;
                var resolvedQuantity = RoundQuantity((resourceQuantity * percentage) / 100m);

                if (balanceLastItem && i == Times.Count - 1)
                    resolvedQuantity = RoundQuantity(resourceQuantity - assignedQuantity);

                if (resolvedQuantity < 0m)
                    resolvedQuantity = 0m;

                Times[i].SetResolvedQuantity(resolvedQuantity);
                assignedQuantity += resolvedQuantity;
            }
        }

        private static decimal RoundMoney(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        private static decimal RoundQuantity(decimal v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);
        private static decimal RoundFactor(decimal v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);
        private static string NormalizeText(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        public ResourceMetadata Clone()
        {
            return new ResourceMetadata
            {
                SchemaVersion = SchemaVersion,
                // Deep copy لتفادي مشاركة نفس الـ reference بين النسخ
                Parameters = Parameters is null
                    ? new()
                    : [.. Parameters.Select(p => new ResourceParameter
                    {
                        Name = p.Name,
                        Unit = p.Unit,
                        Value = p.Value,
                        IsActive = p.IsActive,
                    })],

                AddOns = AddOns is null
                    ? []
                    : [.. AddOns.Select(a => new ResourceAddon
                    {
                        Name = a.Name,
                        Unit = a.Unit,
                        Cost = a.Cost,
                        Factor = a.Factor,
                        Type = a.Type,
                        BaseCost = a.BaseCost,
                        IsActive = a.IsActive,
                    })],

                Times = Times is null
                    ? []
                    : [.. Times.Select(t => new ResourceTime
                    {
                        Name = t.Name,
                        Unit = t.Unit,
                        Percentage = t.Percentage,
                        Cost = t.Cost,
                        IsActive = t.IsActive,
                    }.SetResolvedQuantity(t.Quantity))],
                PriceSub = PriceSub,

                Note = Note ?? string.Empty,
                UpperNote = UpperNote is null ? new() : [.. UpperNote],

                QuantityParam = QuantityParam ?? string.Empty,

                ChangeFactor1 = ChangeFactor1,
                ChangeFactor2 = ChangeFactor2,

                CapWaste = CapWaste,
                CapFromTask = CapFromTask,
                Cap = Cap,
                Waste = Waste,

                Cost = Cost,
                BaseCost = BaseCost,
                CO2 = CO2,
                ImportInfo = ImportInfo ?? string.Empty,
            };
        }
    }

    public class ResourceBase
    {
        public ResourceTypesEnum ResType { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }

        private bool _storedActive = true;
        [JsonIgnore] private bool? _activeOverride;

        public bool Active
        {
            get => _activeOverride ?? _storedActive;
            set => _storedActive = value;
        }

        public void SetActiveOverride(bool? value) => _activeOverride = value;

        [JsonIgnore]
        public int SortOrder
        {
            get => Order;
            set => Order = value;
        }

        [JsonIgnore]
        public bool IsActive
        {
            get => _storedActive;
            set => _storedActive = value;
        }
    }
}
