using System.Collections.Generic;

namespace Domain.DTO.Calculation
{
    public class AdditionalFactorGroupModel
    {
        public string Name { get; set; } = string.Empty;
        public ICollection<AdditionalFactor> AdditionalFactorList { get; set; } = new List<AdditionalFactor>();
    }
    public class AdditionalFactor
    {
        public string Resource { get; set; } = string.Empty;
        public string Name { get; set; } = "Factors Name";
        public decimal Cost { get; set; } = 40m;
        public string Account { get; set; } = string.Empty;
        public bool Risk { get; set; }
        public bool ProfitRatio { get; set; }
        public int Quantity { get; set; } = 1;
        public string Unit { get; set; } = string.Empty;
        public double Time { get; set; } = 1;
        public string TimeUnit { get; set; } = string.Empty;
        public decimal AMP1 { get; set; } = 1m;
        public decimal BaseCost { get; set; } = 1m;
        public string Comment { get; set; } = string.Empty;
    }
}
