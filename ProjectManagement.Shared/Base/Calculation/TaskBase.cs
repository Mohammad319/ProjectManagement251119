using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Base.Calculation.Base;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.Base.Calculation
{
    public class TaskConversionParameter
    {
        [MaxLength(120, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;

        [MaxLength(25, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Unit { get; set; } = string.Empty;

        public decimal Value { get; set; } = 1m;
    }

    public class TaskQuantityConversion
    {
        [MaxLength(25, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string BaseUnit { get; set; } = string.Empty;

        public List<TaskConversionParameter> Parameters { get; set; } = [];
    }

    public class TaskMetadata
    {
        public int SchemaVersion { get; set; } = 1;

        [AllowNull, MaxLength(FieldLengths.Note)]
        public string Note { get; set; } = string.Empty;
        public List<string> UpperNote { get; set; } = [];
        public string QuantityParam { get; set; } = string.Empty;

        // Canonical storage for conversion-related UI data.
        public TaskQuantityConversion Conversion { get; set; } = new();

        // Compatibility wrappers used by existing code paths.
        [JsonIgnore]
        public string BaseUnit
        {
            get => Conversion.BaseUnit;
            set => Conversion.BaseUnit = value ?? string.Empty;
        }

        [JsonIgnore]
        public List<TaskConversionParameter> ConversionParameters
        {
            get
            {
                Conversion.Parameters ??= [];
                return Conversion.Parameters;
            }
            set => Conversion.Parameters = value ?? [];
        }

        public decimal? BaseQuantity { get; set; }

        public decimal ChangeFactor1 { get; set; } = 1;
        public decimal ChangeFactor2 { get; set; } = 1;
        public decimal ActuallyQuantity { get; set; } = 0;
        public decimal WorkedQ { get; set; } = 0;

        [Range(-20, 20, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public decimal? Cap { get; set; }
        public bool IsActive { get; set; } = true;

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Code { get; set; } = string.Empty;

        public TaskType Type { get; set; }
        public bool IsOH { get; set; }
        public bool PriceSubInPrecent { get; set; }
        public decimal? PriceProductionDB { get; set; }
        public decimal? PriceSubDB { get; set; }
        public decimal? PriceSubTaxDB { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? CeilingPrice { get; set; }
        public bool HasVoice { get; set; }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Responsible { get; set; } = string.Empty;

        public void Normalize()
        {
            WorkedQ = RoundQuantity(WorkedQ);
            BaseQuantity = BaseQuantity.HasValue ? RoundQuantity(BaseQuantity.Value) : null;
            BaseUnit = NormalizeText(BaseUnit);
            ChangeFactor1 = RoundFactor(ChangeFactor1);
            ChangeFactor2 = RoundFactor(ChangeFactor2);

            if (ConversionParameters is null)
                ConversionParameters = [];

            for (int i = 0; i < ConversionParameters.Count; i++)
            {
                var parameter = ConversionParameters[i];
                if (parameter is null)
                    continue;

                parameter.Name = NormalizeText(parameter.Name);
                parameter.Unit = NormalizeText(parameter.Unit);
                parameter.Value = RoundFactor(parameter.Value);
            }

            PriceProductionDB = PriceProductionDB.HasValue ? RoundMoney(PriceProductionDB.Value) : null;
            PriceSubDB = PriceSubDB.HasValue ? RoundMoney(PriceSubDB.Value) : null;
            PriceSubTaxDB = PriceSubTaxDB.HasValue ? RoundMoney(PriceSubTaxDB.Value) : null;
            MinPrice = MinPrice.HasValue ? RoundMoney(MinPrice.Value) : null;
            CeilingPrice = CeilingPrice.HasValue ? RoundMoney(CeilingPrice.Value) : null;

            if (WorkedQ < 0m) WorkedQ = 0m;
            if (BaseQuantity.HasValue && BaseQuantity.Value < 0m) BaseQuantity = 0m;
            if (PriceProductionDB.HasValue && PriceProductionDB.Value < 0m) PriceProductionDB = 0m;
            if (PriceSubDB.HasValue && PriceSubDB.Value < 0m) PriceSubDB = 0m;
            if (PriceSubTaxDB.HasValue && PriceSubTaxDB.Value < 0m) PriceSubTaxDB = 0m;
            if (MinPrice.HasValue && MinPrice.Value < 0m) MinPrice = 0m;
            if (CeilingPrice.HasValue && CeilingPrice.Value < 0m) CeilingPrice = 0m;
        }

        public void SyncConversionFactorFromParameters()
        {
            if (ConversionParameters is null || ConversionParameters.Count == 0)
                return;

            decimal product = 1m;
            for (int i = 0; i < ConversionParameters.Count; i++)
                product *= ConversionParameters[i].Value;

            ChangeFactor2 = RoundFactor(product);
        }

        private static decimal RoundMoney(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        private static decimal RoundQuantity(decimal v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);
        private static decimal RoundFactor(decimal v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);
        private static string NormalizeText(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        public TaskMetadata Clone()
        {
            return new TaskMetadata
            {
                SchemaVersion = SchemaVersion,
                Note = Note ?? string.Empty,
                UpperNote = UpperNote is null ? new() : new List<string>(UpperNote),
                QuantityParam = QuantityParam ?? string.Empty,
                Conversion = new TaskQuantityConversion
                {
                    BaseUnit = Conversion?.BaseUnit ?? string.Empty,
                    Parameters = Conversion?.Parameters is null
                        ? []
                        : [.. Conversion.Parameters.Select(p => new TaskConversionParameter
                        {
                            Name = p.Name,
                            Unit = p.Unit,
                            Value = p.Value
                        })]
                },
                BaseQuantity = BaseQuantity,
                ChangeFactor1 = ChangeFactor1,
                ChangeFactor2 = ChangeFactor2,
                ActuallyQuantity = ActuallyQuantity,
                WorkedQ = WorkedQ,
                Cap = Cap,
                IsActive = IsActive,
                Code = Code ?? string.Empty,
                Type = Type,
                IsOH = IsOH,
                PriceSubInPrecent = PriceSubInPrecent,
                PriceProductionDB = PriceProductionDB,
                PriceSubDB = PriceSubDB,
                PriceSubTaxDB = PriceSubTaxDB,
                MinPrice = MinPrice,
                CeilingPrice = CeilingPrice,
                HasVoice = HasVoice,
                Responsible = Responsible ?? string.Empty,
            };
        }
    }

    public enum TaskType
    {
        Task, FixedQ, Minus, CodeName
    }

    public class TaskBase
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(FieldLengths.TaskName, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }

        [JsonIgnore]
        public int SortOrder
        {
            get => Order;
            set => Order = value;
        }
    }
}
