using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Constants;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Calculation
{
    public sealed class TemplateMetadataData
    {
        public int MathRound { get; set; } = 2;

        [MaxLength(5)]
        public string Currency { get; set; } = "â‚¬";

        [MaxLength(15)]
        public string DateFormat { get; set; } = "dd.MM.yyyy";

        public NetCalcStyleData NetCalc { get; set; } = new();
        public SummarySheet SummarySheet { get; set; } = new();
    }

    public sealed class NetCalcStyleData
    {
        public NetColor Color { get; set; } = new();
    }
}
