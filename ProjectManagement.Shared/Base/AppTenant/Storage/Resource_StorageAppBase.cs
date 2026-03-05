using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System;

namespace ProjectManagement.Shared.Base.AppTenant.Storage
{
    public class Resource_StorageAppBase
    {
        [MaxLength(25, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Unit { get; set; } = string.Empty;
        [Required] public decimal Cost { get; set; }
        public decimal? FixedQ { get; set; }

        public decimal ChangeFactor1 { get; set; } = 1m;
        public decimal ChangeFactor2 { get; set; } = 1m;
        public decimal CapWaste { get; set; }
        public double? CO2 { get; set; }
        [AllowNull, MaxLength(500)] public string Note { get; set; } = string.Empty;
        public string UpperNote { get; set; } = string.Empty;
        public void Normalize()
        {
            Cost = RoundMoney(Cost);
            FixedQ = FixedQ.HasValue ? RoundQuantity(FixedQ.Value) : null;

            ChangeFactor1 = RoundFactor(ChangeFactor1);
            ChangeFactor2 = RoundFactor(ChangeFactor2);
            CapWaste = RoundFactor(CapWaste);
        }

        private static decimal RoundMoney(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        private static decimal RoundQuantity(decimal v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);
        private static decimal RoundFactor(decimal v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);

    }
}
