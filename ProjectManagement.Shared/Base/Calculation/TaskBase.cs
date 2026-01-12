using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Base.Calculation.Base;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

namespace ProjectManagement.Shared.Base.Calculation
{
    public class TaskMetadata
    {
        [AllowNull, MaxLength(500)]
        public string Note { get; set; }
        public List<string> UpperNote { get; set; } = [];
        public string QuantityParam { get; set; }
        public double? Quantity { get; set; }
        [MaxLength(25, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Unit { get; set; }
        public double ChangeFactor1 { get; set; } = 1;
        public double ChangeFactor2 { get; set; } = 1;
        public double ActuallyQuantity { get; set; } = 0;
        public double WorkedQ { get; set; } = 0;

        [Range(-20, 20, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public double? Cap { get; set; }
        public bool IsActive { get; set; } = true;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Code { get; set; }
        public TaskType Type { get; set; }

        public bool IsOH { get; set; }
        public bool PriceSubInPrecent { get; set; }
        public double? PriceSubDB { get; set; }
        public double? PriceSubTaxDB { get; set; }
        public double? MinPrice { get; set; }
        public double? CeilingPrice { get; set; }
        public bool HasVoice { get; set; }
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Responsible { get; set; }

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
        public string Name { get; set; }
        public double Order { get; set; }
    }
}
