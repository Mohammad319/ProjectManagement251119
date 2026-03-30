using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public class TemplateMVVM : TemplateData
    {
        public double Format(double x) => UiStyles.Format(x, MathRound);
        public decimal Format(decimal x) => UiStyles.Format(x, MathRound);
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Legacy compatibility hook: frozen-column positioning now comes from the grid DOM.
        public string StyleNetCalc { get; set; } = string.Empty;
        public int StartCol1 { get; set; }

        public string FreezCol()
        {
            StyleNetCalc = string.Empty;
            return StyleNetCalc;
        }

        public string FreezColtodelete() => FreezCol();
    }
}
