using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Base.Calculation.Base;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System;

namespace ProjectManagement.Shared.Base.Calculation
{
    public class TaskMetadata
    {
        [AllowNull, MaxLength(500)]
        public string Note { get; set; } = string.Empty;
        public List<string> UpperNote { get; set; } = [];
        public string QuantityParam { get; set; } = string.Empty;
        public decimal? Quantity { get; set; }
        [MaxLength(25, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Unit { get; set; } = string.Empty;
        public decimal ChangeFactor1 { get; set; } = 1;
        public decimal ChangeFactor2 { get; set; } = 1;
        public decimal ActuallyQuantity { get; set; } = 0;
        // Worked quantity — نستخدم decimal لتفادي أخطاء الدقة مع الحسابات المالية
        public decimal WorkedQ { get; set; } = 0;

        [Range(-20, 20, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public decimal? Cap { get; set; }
        public bool IsActive { get; set; } = true;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Code { get; set; } = string.Empty;
        public TaskType Type { get; set; }

        public bool IsOH { get; set; }
        public bool PriceSubInPrecent { get; set; }
        public decimal? PriceSubDB { get; set; }
        public decimal? PriceSubTaxDB { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? CeilingPrice { get; set; }
        public bool HasVoice { get; set; }
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Responsible { get; set; } = string.Empty;
        
        /// <summary>
        /// توحيد القيم (خصوصًا الأسعار) لمنع كسور طويلة + منع قيم سالبة للمال.
        /// </summary>
        public void Normalize()
        {
            WorkedQ = RoundQuantity(WorkedQ);
            Quantity = Quantity.HasValue ? RoundQuantity(Quantity.Value) : null;

            // money-like fields
            PriceSubDB = PriceSubDB.HasValue ? RoundMoney(PriceSubDB.Value) : null;
            PriceSubTaxDB = PriceSubTaxDB.HasValue ? RoundMoney(PriceSubTaxDB.Value) : null;
            MinPrice = MinPrice.HasValue ? RoundMoney(MinPrice.Value) : null;
            CeilingPrice = CeilingPrice.HasValue ? RoundMoney(CeilingPrice.Value) : null;

            // clamp negatives where it doesn't make sense
            if (WorkedQ < 0m) WorkedQ = 0m;
            if (Quantity.HasValue && Quantity.Value < 0m) Quantity = 0m;
            if (PriceSubDB.HasValue && PriceSubDB.Value < 0m) PriceSubDB = 0m;
            if (PriceSubTaxDB.HasValue && PriceSubTaxDB.Value < 0m) PriceSubTaxDB = 0m;
            if (MinPrice.HasValue && MinPrice.Value < 0m) MinPrice = 0m;
            if (CeilingPrice.HasValue && CeilingPrice.Value < 0m) CeilingPrice = 0m;
        }

        private static decimal RoundMoney(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        private static decimal RoundQuantity(decimal v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);

public TaskMetadata Clone()
        {
            return new TaskMetadata
            {
                Note = Note ?? string.Empty,
                UpperNote = UpperNote is null ? new() : new List<string>(UpperNote),

                QuantityParam = QuantityParam ?? string.Empty,
                Quantity = Quantity,
                Unit = Unit ?? string.Empty,

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
        Task,FixedQ, Minus,CodeName
    }
    public class TaskBase
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}